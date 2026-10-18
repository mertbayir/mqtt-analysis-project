# Adım 5: bağlantı sonrası yük ve canlı alarm gecikmesi.
# Cihazlar vital bulgu ve alarm üretir; kesintide biriktirir; bağlanınca politikaya (B) göre eşitler/boşaltır.
# Toplayıcı (ayrı konteyner) tüm mesajların gecikmesini ölçer.
# Zaman çizelgesi (simülatör başladıktan itibaren): YayilimSn sürede ilk bağlanma, OncesiSn'de sunucu durur,
# KesintiSn sonra başlar, SonrasiSn daha izlenir.
# Kullanım: ./scripts/adim5.ps1 -A ustel -B b1 -Cihaz 500 [-Tohum 1] [-Surum p1] [-Not "..."]
# Çıktılar results/ altında: <ad>.csv/.girdi.json/.olay.csv/.toplam.json (simülatör), <ad>.collector.csv(+.toplam.json),
#   <ad>.konsol.txt, <ad>.olaylar.txt, <ad>.broker.csv, <ad>.sim.csv ve deney_kayitlari.csv satırı.
param(
    [ValidateSet("sabit", "ustel", "ustel-rastgele")] [string]$A = "ustel",
    [ValidateSet("b0", "b1", "b2", "b3")] [string]$B = "b1",
    [int]$Cihaz = 500,
    [int]$Tohum = 1,
    [int]$YayilimSn = 20,
    [int]$OncesiSn = 40,
    [int]$KesintiSn = 60,
    [int]$SonrasiSn = 90,
    [int]$VitalSn = 2,
    [int]$AlarmSn = 10,
    [int]$MesajBayt = 128,
    [int]$SyncBayt = 4096,
    [int]$B2PencereSn = 30,
    [int]$B2AralikMs = 200,
    [string]$Surum = "p1",
    [switch]$AdlaBaglan,
    [ValidateSet("temiz", "sessiz")] [string]$KesintiTuru = "temiz",   # Adım 9: temiz = docker stop/start; sessiz = ağdan kopar (docker network disconnect)
    [int]$AckSn = 0,   # Adım 9: yayın onayı zaman aşımı (sn); 0 = yok. Sessiz kesintide cihazın kopmayı fark etmesi için gerekir
    [switch]$Tls,         # Adım 6: cihazlar sunucuya TLS ile (8883) bağlanır; toplayıcı TLS'siz kalır
    [ValidateSet("mosquitto", "emqx")] [string]$Sunucu = "mosquitto",   # Adım 7: hangi MQTT sunucusu
    [int]$SunucuCpu = 2,   # EMQX için sunucu CPU payı (Mosquitto tek çekirdek kullanır)
    [switch]$HamKayit,   # toplayıcı her mesajı ham dosyaya yazar (kayıp analizi için; büyük dosya, git dışı)
    [string]$Not = ""
)
if ($Tls) { $Not = "TLS (RSA-2048, port 8883, sertifika dogrulanmaz). " + $Not }
$ErrorActionPreference = "Stop"
$ci = [Globalization.CultureInfo]::InvariantCulture
$proje = Split-Path $PSScriptRoot -Parent
Set-Location $proje
$env:BROKER_CPUS = "$SunucuCpu"
if ($Sunucu -eq "emqx") { $env:COMPOSE_FILE = "docker-compose.yml;docker-compose.emqx.yml" } else { Remove-Item Env:COMPOSE_FILE -ErrorAction SilentlyContinue }
$klasor = Join-Path $proje "results"
New-Item -ItemType Directory -Force $klasor | Out-Null

# B2 varsayılan dışı parametreyle koşulursa etiket parametreyi taşır (ayrı hücre olarak analiz edilir): b2-w60i1000
$bEtiket = if (($B -eq "b2" -or $B -eq "b3") -and ($B2PencereSn -ne 30 -or $B2AralikMs -ne 200)) { "${B}-w${B2PencereSn}i${B2AralikMs}" } else { $B }
if ($KesintiTuru -eq "sessiz") { $bEtiket = "$bEtiket-sessiz" }
if ($KesintiSn -ne 60) { $bEtiket = "$bEtiket-k$KesintiSn" }   # Adım 10: kesinti süresi taraması
$ad = "adim5_${A}_${bEtiket}_n${Cihaz}_s${Tohum}_${Surum}"
$sureSn = $OncesiSn + $KesintiSn + $SonrasiSn + 5
$colSureSn = $sureSn + 30
$commit = (git rev-parse --short HEAD).Trim()
$kirli = [bool](git status --porcelain -- src scripts docker-compose.yml docker-compose.emqx.yml mosquitto)
$csv = "$ad.csv"; $girdi = Join-Path $klasor "$ad.girdi.json"
$log = Join-Path $klasor "$ad.konsol.txt"; $olay = Join-Path $klasor "$ad.olaylar.txt"
$brokerCsv = Join-Path $klasor "$ad.broker.csv"; $simCsv = Join-Path $klasor "$ad.sim.csv"
$colLog = Join-Path $klasor "$ad.collector.konsol.txt"
Remove-Item $girdi, $log, $olay, $brokerCsv, $simCsv, $colLog, (Join-Path $klasor $csv) -ErrorAction SilentlyContinue

# Her koşuda yığını temiz kur
docker compose --profile sim down --remove-orphans | Out-Null
docker compose up -d broker | Out-Null
Start-Sleep 3
docker compose --profile sim build sim collector | Out-Null
$sunucuAdr = (docker inspect mqtt-broker --format '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}').Trim()
$sunucuIp = $sunucuAdr
if ($AdlaBaglan) { $sunucuAdr = "broker" }

# Sunucu ve simülatör kaynak kullanımı örnekleyicileri (arka planda)
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
$sampler2 = Start-Job -ArgumentList $simCsv, "mqtt-sim" -ScriptBlock $ornekleyici

# Toplayıcıyı önce başlat, abone olmasını bekle
$colArgs = @("compose", "run", "--rm", "--name", "mqtt-collector",
    "-e", "BROKER_HOST=$sunucuAdr", "-e", "DURATION_SEC=$colSureSn", "-e", "SYNC_BYTES=$SyncBayt",
    "-e", "OUT_PATH=/results/$ad.collector.csv")
if ($HamKayit) { $colArgs += @("-e", "RAW_PATH=/results/$ad.collector.ham.csv") }
$colArgs += "collector"
$col = Start-Process docker -ArgumentList $colArgs -NoNewWindow -PassThru -RedirectStandardOutput $colLog
$bekle = 0
while (-not ((Test-Path $colLog) -and ((Get-Content $colLog -Encoding UTF8 -ErrorAction SilentlyContinue) -match "abone oldu"))) {
    Start-Sleep -Milliseconds 300; $bekle++; if ($bekle -gt 200) { throw "toplayıcı başlamadı" }
}

$simArgs = @("compose", "run", "--rm", "--name", "mqtt-sim",
    "-e", "POLICY=$A", "-e", "POLICY_B=$B", "-e", "DEVICES=$Cihaz", "-e", "DURATION_SEC=$sureSn", "-e", "SEED=$Tohum",
    "-e", "START_SPREAD_SEC=$YayilimSn", "-e", "BROKER_HOST=$sunucuAdr", "-e", "CSV_PATH=/results/$csv",
    "-e", "VITAL_SEC=$VitalSn", "-e", "ALARM_SEC=$AlarmSn", "-e", "MSG_BYTES=$MesajBayt",
    "-e", "B2_WINDOW_SEC=$B2PencereSn", "-e", "B2_INTERVAL_MS=$B2AralikMs", "-e", "ACK_TIMEOUT_SEC=$AckSn", "sim")
if ($Tls) { $simArgs = @($simArgs[0..($simArgs.Count - 2)]) + @("-e", "TLS=1", "-e", "BROKER_PORT=8883", "sim") }
$sim = Start-Process docker -ArgumentList $simArgs -NoNewWindow -PassThru -RedirectStandardOutput $log

$bekle = 0
while (-not (Test-Path $girdi)) { Start-Sleep -Milliseconds 200; $bekle++; if ($bekle -gt 300) { throw "simülatör başlamadı" } }
$tSim = Get-Date
"$($tSim.ToString('HH:mm:ss.fff'))  simulator basladi" | Tee-Object -FilePath $olay

Start-Sleep -Seconds $OncesiSn
$tStop = Get-Date
"$($tStop.ToString('HH:mm:ss.fff'))  docker stop" | Tee-Object -FilePath $olay -Append
if ($KesintiTuru -eq "sessiz") { docker network disconnect project_default mqtt-broker | Out-Null } else { docker stop mqtt-broker | Out-Null }
"$((Get-Date).ToString('HH:mm:ss.fff'))  docker stop bitti (kesinti=$KesintiTuru)" | Tee-Object -FilePath $olay -Append
Start-Sleep -Seconds $KesintiSn
$tStart = Get-Date
"$($tStart.ToString('HH:mm:ss.fff'))  docker start" | Tee-Object -FilePath $olay -Append
if ($KesintiTuru -eq "sessiz") { docker network connect --ip $sunucuIp --alias broker project_default mqtt-broker | Out-Null } else { docker start mqtt-broker | Out-Null }

# (Windows'tan yalnızca yayımlanan porta ulaşılır, kapsayıcı IP'sine değil.)
# Sunucunun bağlantı kabul etmeye başladığı an: "docker start" komutundan, yayımlanan 1883 portu (127.0.0.1) açılana kadar geçen süre.
# Mosquitto ~0,5 sn, EMQX (Erlang VM açılışı) daha uzun olabilir; TTR bu açılış süresini de içerir (sunucunun bir özelliği).
$hSw = [Diagnostics.Stopwatch]::StartNew()
$hazirSn = -1
while ($hSw.Elapsed.TotalSeconds -lt 60) {
    try { $tc = New-Object Net.Sockets.TcpClient; $tc.Connect("127.0.0.1", 1883); $tc.Close(); $hazirSn = [math]::Round(((Get-Date) - $tStart).TotalSeconds, 2); break } catch { Start-Sleep -Milliseconds 20 }
}
"$((Get-Date).ToString('HH:mm:ss.fff'))  sunucu portu acik (docker start'tan sonra ${hazirSn} sn)" | Tee-Object -FilePath $olay -Append

$sim.WaitForExit()
$col.WaitForExit()
Stop-Job $sampler, $sampler2; Remove-Job $sampler, $sampler2

# Kayıt defterine satır ekle
$kayit = Join-Path $klasor "deney_kayitlari.csv"
$kapali = ($tStart - $tStop).TotalSeconds.ToString("0.0", $ci)
$ayrinti = "A=$A B=$B vital=${VitalSn}sn alarm=${AlarmSn}sn mesaj=${MesajBayt}B sync=${SyncBayt}B b2=U(0,${B2PencereSn})sn+${B2AralikMs}ms. $Not"
$satir = @($ad, (Get-Date -Format "yyyy-MM-dd HH:mm"), $commit, $kirli, "$A|$bEtiket", $Cihaz, $Tohum, $sureSn,
    $KesintiTuru, $kapali, $tStop.ToString("HH:mm:ss.fff"), $tStart.ToString("HH:mm:ss.fff"), $(if ($Sunucu -eq "emqx") { "emqx:5.8.6" } else { "mosquitto:2.1.2" }),
    ("`"$SunucuCpu CPU, 2 GB (konteyner sinirli)`""), ("`"docker-konteyner (adres=${sunucuAdr}:1883), sim 4 CPU 3 GB, toplayici 2 CPU 1 GB`""), $YayilimSn,
    ('"' + $ayrinti.Replace('"', "'") + '"')) -join ","
Add-Content -Path $kayit -Value $satir -Encoding utf8
Write-Host "Bitti: $ad (kod=$commit, değişiklik var=$kirli, kapalı=$kapali sn)"
