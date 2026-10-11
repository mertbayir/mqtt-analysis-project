# Adım 2: 100 cihaz bağlanır, 15. sn'de sunucu durdurulur, 20 sn kapalı kalır, sonra açılır.
$ErrorActionPreference = "Stop"
$proje = Split-Path $PSScriptRoot -Parent
Set-Location $proje

docker compose up -d broker | Out-Null
dotnet build src/Simulator -c Release --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "derleme hatası" }

$env:DEVICES = "100"
$env:DURATION_SEC = "75"
$env:SEED = "1"
$env:CSV_PATH = Join-Path $proje "results/adim2.csv"

$log = Join-Path $proje "results/adim2_konsol.txt"
New-Item -ItemType Directory -Force (Split-Path $log) | Out-Null

$sim = Start-Process dotnet -ArgumentList "src/Simulator/bin/Release/net8.0/Simulator.dll" `
    -NoNewWindow -PassThru -RedirectStandardOutput $log

Start-Sleep 15
"$(Get-Date -Format HH:mm:ss.fff)  docker stop" | Tee-Object -FilePath (Join-Path $proje "results/adim2_olaylar.txt")
docker stop mqtt-broker | Out-Null
Start-Sleep 20
"$(Get-Date -Format HH:mm:ss.fff)  docker start" | Tee-Object -FilePath (Join-Path $proje "results/adim2_olaylar.txt") -Append
docker start mqtt-broker | Out-Null

$sim.WaitForExit()
Get-Content $log
