# Adım 2 karşılaştırması: N cihaz bağlanır, 15. sn'de sunucu durdurulur, 20 sn kapalı kalır, sonra açılır.
# Kullanım: ./scripts/adim2.ps1 -Politika sabit|ustel|ustel-rastgele [-Cihaz 100] [-Tohum 1]
param(
    [ValidateSet("sabit", "ustel", "ustel-rastgele")] [string]$Politika = "sabit",
    [int]$Cihaz = 100,
    [int]$Tohum = 1,
    [int]$SureSn = 100
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
"$(Get-Date -Format HH:mm:ss.fff)  docker stop" | Tee-Object -FilePath $olay
docker stop mqtt-broker | Out-Null
Start-Sleep 20
"$(Get-Date -Format HH:mm:ss.fff)  docker start" | Tee-Object -FilePath $olay -Append
docker start mqtt-broker | Out-Null

$sim.WaitForExit()
Write-Host "Bitti: $ad"
