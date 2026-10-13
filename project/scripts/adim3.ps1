# Adım 3: büyük N ile temiz kesinti. Simülatör Docker içinde, sunucu sabit kaynak sınırıyla.
# Zaman çizelgesi (simülatör başladıktan itibaren):
#   0 .. YayilimSn      cihazlar yayılarak ilk kez bağlanır
#   OncesiSn            sunucu durdurulur (temiz kesinti)
#   OncesiSn + KesintiSn sunucu başlatılır
#   ... + SonrasiSn     koşu biter
# Kullanım: ./scripts/adim3.ps1 -Politika sabit -Cihaz 1000 [-Tohum 1] [-Not "..."]
# Çıktılar results/ altında: <ad>.csv, .girdi.json, .konsol.txt, .olaylar.txt, .broker.csv (+ deney_kayitlari.csv satırı)
param(
    [ValidateSet("sabit", "ustel", "ustel-rastgele")] [string]$Politika = "sabit",
    [int]$Cihaz = 1000,
    [int]$Tohum = 1,
    [int]$YayilimSn = 20,
    [int]$OncesiSn = 40,
    [int]$KesintiSn = 60,
    [int]$SonrasiSn = 120,
    [string]$Not = "",
    [string]$Surum = "v2"   # kod/yöntem sürümü; yöntem değişince artırılır (v1: zaman aşımı uygulanmıyordu, geçersiz)
,    [switch]$IpIleBaglan   # cihazlar sunucuya adıyla (broker) değil IP adresiyle bağlanır (DNS etkisini ayırmak için)
)
$ErrorActionPreference = "Stop"
$ci = [Globalization.CultureInfo]::InvariantCulture
$proje = Split-Path $PSScriptRoot -Parent
Set-Location $proje
$klasor = Join-Path $proje "results"
New-Item -ItemType Directory -Force $klasor | Out-Null

$ad = "adim3_${Politika}_n${Cihaz}_s${Tohum}_${Surum}"
$sureSn = $OncesiSn + $KesintiSn + $SonrasiSn + 5
$commit = (git rev-parse --short HEAD).Trim()
$kirli = [bool](git status --porcelain -- src scripts docker-compose.yml mosquitto)
$csv = "$ad.csv"; $girdi = Join-Path $klasor "$ad.girdi.json"
$log = Join-Path $klasor "$ad.konsol.txt"; $olay = Join-Path $klasor "$ad.olaylar.txt"; $brokerCsv = Join-Path $klasor "$ad.broker.csv"
Remove-Item $girdi, $log, $olay, $brokerCsv, (Join-Path $klasor $csv) -ErrorAction SilentlyContinue

# Her koşuda yığını temiz kur (önceki koşudan kalıntı olmasın)
docker compose --profile sim down --remove-orphans | Out-Null
docker compose up -d broker | Out-Null
Start-Sleep 3
$sunucuAdr = "broker"
if ($IpIleBaglan) { $sunucuAdr = (docker inspect mqtt-broker --format '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}').Trim() }
docker compose --profile sim build sim | Out-Null

# Sunucu kaynak kullanımı örnekleyicisi (arka planda)
$ornekleyici = {
    param($yol, $kap)
    "zaman,cpu_yuzde,bellek" | Out-File $yol -Encoding utf8
    while ($true) {
        $s = docker stats --no-stream --format "{{.CPUPerc}};{{.MemUsage}}" $kap 2>$null
        if ($s) {
            $p = $s -split ";"
            "$((Get-Date).ToString('HH:mm:ss.fff')),$($p[0].Trim().TrimEnd('%')),$($p[1].Split('/')[0].Trim())" | Add-Content $yol -Encoding utf8
        }
        Start-Sleep -Milliseconds 200
    }
}
$sampler = Start-Job -ArgumentList $brokerCsv, "mqtt-broker" -ScriptBlock $ornekleyici
$simCsv = Join-Path $klasor "$ad.sim.csv"
$sampler2 = Start-Job -ArgumentList $simCsv, "mqtt-sim" -ScriptBlock $ornekleyici

$dockerArgs = @("compose", "run", "--rm", "--name", "mqtt-sim",
    "-e", "POLICY=$Politika", "-e", "DEVICES=$Cihaz", "-e", "DURATION_SEC=$sureSn", "-e", "SEED=$Tohum",
    "-e", "START_SPREAD_SEC=$YayilimSn", "-e", "BROKER_HOST=$sunucuAdr", "-e", "CSV_PATH=/results/$csv", "sim")
$sim = Start-Process docker -ArgumentList $dockerArgs -NoNewWindow -PassThru -RedirectStandardOutput $log

# Simülatör gerçekten başlayana kadar bekle (girdi.json başlangıçta yazılır)
$bekle = 0
while (-not (Test-Path $girdi)) { Start-Sleep -Milliseconds 200; $bekle++; if ($bekle -gt 300) { throw "simülatör başlamadı" } }
$tSim = Get-Date
"$($tSim.ToString('HH:mm:ss.fff'))  simulator basladi" | Tee-Object -FilePath $olay

Start-Sleep -Seconds $OncesiSn
$tStop = Get-Date
"$($tStop.ToString('HH:mm:ss.fff'))  docker stop" | Tee-Object -FilePath $olay -Append
docker stop mqtt-broker | Out-Null
Start-Sleep -Seconds $KesintiSn
$tStart = Get-Date
"$($tStart.ToString('HH:mm:ss.fff'))  docker start" | Tee-Object -FilePath $olay -Append
docker start mqtt-broker | Out-Null

$sim.WaitForExit()
Stop-Job $sampler, $sampler2; Remove-Job $sampler, $sampler2

# Kayıt defterine satır ekle
$kayit = Join-Path $klasor "deney_kayitlari.csv"
$kapali = ($tStart - $tStop).TotalSeconds.ToString("0.0", $ci)
$satir = @($ad, (Get-Date -Format "yyyy-MM-dd HH:mm"), $commit, $kirli, $Politika, $Cihaz, $Tohum, $sureSn,
    "temiz", $kapali, $tStop.ToString("HH:mm:ss.fff"), $tStart.ToString("HH:mm:ss.fff"), "mosquitto:2.1.2",
    '"2 CPU, 2 GB (konteyner sinirli)"', ("`"docker-konteyner (adres=${sunucuAdr}:1883), sim 4 CPU 3 GB`""), $YayilimSn,
    ('"' + $Not.Replace('"', "'") + '"')) -join ","
Add-Content -Path $kayit -Value $satir -Encoding utf8
Write-Host "Bitti: $ad (kod=$commit, değişiklik var=$kirli, kapalı=$kapali sn)"





