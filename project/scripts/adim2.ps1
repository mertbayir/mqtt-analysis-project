# Adım 2 karşılaştırması: N cihaz bağlanır, 15. sn'de sunucu durdurulur, ~20 sn kapalı kalır, sonra açılır.
# Kullanım: ./scripts/adim2.ps1 -Politika sabit|ustel|ustel-rastgele [-Cihaz 100] [-Tohum 1] [-Not "..."]
# Her koşu şunları üretir (results/ altında):
#   <ad>.csv           saniyelik özet (bağlı, deneme, hata)
#   <ad>.girdi.json    koşunun tüm girdileri (simülatör yazar)
#   <ad>.konsol.txt    simülatör konsol çıktısı
#   <ad>.olaylar.txt   docker stop/start zamanları
# ve deney_kayitlari.csv'ye bir satır eklenir (kod sürümü dahil).
param(
    [ValidateSet("sabit", "ustel", "ustel-rastgele")] [string]$Politika = "sabit",
    [int]$Cihaz = 100,
    [int]$Tohum = 1,
    [int]$SureSn = 100,
    [string]$Not = ""
)
$ErrorActionPreference = "Stop"
$proje = Split-Path $PSScriptRoot -Parent
Set-Location $proje

docker compose up -d broker | Out-Null
dotnet build src/Simulator -c Release --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "derleme hatası" }

$ad = "adim2_${Politika}_n${Cihaz}_s${Tohum}"
$klasor = Join-Path $proje "results"
New-Item -ItemType Directory -Force $klasor | Out-Null

# Hangi kodla çalıştırıldı? (kayıt için)
$commit = (git rev-parse --short HEAD).Trim()
$kirli = [bool](git status --porcelain -- src scripts docker-compose.yml mosquitto)

$env:POLICY = $Politika
$env:DEVICES = "$Cihaz"
$env:DURATION_SEC = "$SureSn"
$env:SEED = "$Tohum"
$env:CSV_PATH = Join-Path $klasor "$ad.csv"
$log = Join-Path $klasor "$ad.konsol.txt"
$olay = Join-Path $klasor "$ad.olaylar.txt"

$sim = Start-Process dotnet -ArgumentList "src/Simulator/bin/Release/net8.0/Simulator.dll" `
    -NoNewWindow -PassThru -RedirectStandardOutput $log

Start-Sleep 15
$tStop = Get-Date
"$($tStop.ToString('HH:mm:ss.fff'))  docker stop" | Tee-Object -FilePath $olay
docker stop mqtt-broker | Out-Null
Start-Sleep 20
$tStart = Get-Date
"$($tStart.ToString('HH:mm:ss.fff'))  docker start" | Tee-Object -FilePath $olay -Append
docker start mqtt-broker | Out-Null

$sim.WaitForExit()

# Kayıt defterine satır ekle
$kayit = Join-Path $klasor "deney_kayitlari.csv"
if (-not (Test-Path $kayit)) {
    "kosu,tarih,git_commit,kod_degisikligi_var,politika,cihaz,tohum,sure_sn,kesinti_turu,kapali_sn,stop_zamani,start_zamani,sunucu,sunucu_kaynak,sim_yeri,ilk_yayilim_sn,not" |
        Out-File $kayit -Encoding utf8
}
$kapali = [math]::Round(($tStart - $tStop).TotalSeconds, 1)
# Türkçe yerel ayarda ondalık ayraç virgül olur ve CSV bozulur, o yüzden InvariantCulture
$kapaliYazi = $kapali.ToString("0.0", [Globalization.CultureInfo]::InvariantCulture)
$satir = @($ad, (Get-Date -Format "yyyy-MM-dd HH:mm"), $commit, $kirli, $Politika, $Cihaz, $Tohum, $SureSn,
    "temiz", $kapaliYazi, $tStop.ToString("HH:mm:ss.fff"), $tStart.ToString("HH:mm:ss.fff"),
    "mosquitto:2.1.2", '"sinirsiz (Docker: 12 CPU, ~8 GB)"', '"windows-host (localhost:1883)"', "0",
    ('"' + $Not.Replace('"', "'") + '"')) -join ","
Add-Content -Path $kayit -Value $satir -Encoding utf8
Write-Host "Bitti: $ad (kod=$commit, değişiklik var=$kirli, kapalı=$kapali sn)"
