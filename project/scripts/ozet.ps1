# Koşuların özet tablosunu üretir (makaledeki sayılar buradan gelir, elle hesaplanmaz).
# Kullanım: ./scripts/ozet.ps1 [-Onek adim3]            -> ekrana yazar
#           ./scripts/ozet.ps1 -Onek adim3 | Out-File results/ozet_adim3.txt -Encoding utf8
#
# Tanımlar:
#  kopma        : bağlı cihaz sayısının, en az %90'ına ulaştıktan SONRA ilk kez 0 olduğu saniye
#  tepe_deneme  : kesintide (kopma .. sunucu başlatma) bir saniyede yapılan en çok bağlantı denemesi
#  tepe_sonra   : sunucu başlatıldıktan sonraki en çok deneme/sn (fırtına belirtisi)
#  hata_sonra   : sunucu başlatıldıktan sonraki toplam başarısız bağlantı (sunucu AÇIKKEN yaşanan hatalar = aşırı yük belirtisi)
#  TTR 50/95/100: sunucu başlatıldıktan sonra bağlı cihaz oranının ilk kez %50/%95/%100'e ulaştığı süre (sn, 1 sn çözünürlük)
#  srv_cpu_tepe : sunucu konteynerinin en yüksek CPU'su (% , 100 = 1 çekirdek, 200 = 2 çekirdek = sınır), varsa
param([string]$Onek = "adim")
$ci = [Globalization.CultureInfo]::InvariantCulture
$klasor = Join-Path (Split-Path $PSScriptRoot -Parent) "results"
$kayitlar = Import-Csv (Join-Path $klasor "deney_kayitlari.csv") | Where-Object { $_.kosu -like "$Onek*" }

"{0,-32} {1,6} {2,6} {3,7} {4,11} {5,10} {6,10} {7,10} {8,6} {9,6} {10,6} {11,12}" -f "kosu","cihaz","kopma","kapali","tepe_deneme","tepe_sonra","hata_sonra","toplam_den","TTR50","TTR95","TTR100","srv_cpu_tepe"
foreach ($k in $kayitlar) {
    $csv = Join-Path $klasor "$($k.kosu).csv"
    if (-not (Test-Path $csv)) { continue }
    $r = Import-Csv $csv
    $n = [int]$k.cihaz
    $kapali = [double]::Parse($k.kapali_sn, $ci)

    # kopma: %90'a ulaşıldıktan sonra ilk sıfır
    $ulasti = $false; $kopma = $null
    foreach ($s in $r) {
        if ([int]$s.bagli -ge [math]::Ceiling($n * 0.9)) { $ulasti = $true }
        if ($ulasti -and [int]$s.bagli -eq 0) { $kopma = [int]$s.saniye; break }
    }
    if ($null -eq $kopma) { "{0,-32} kopma tespit edilemedi" -f $k.kosu; continue }
    $basla = $kopma + $kapali

    $kesinti = $r | Where-Object { [int]$_.saniye -ge $kopma -and [int]$_.saniye -le $basla }
    $sonra = $r | Where-Object { [int]$_.saniye -gt $basla }
    $tepe = ($kesinti | ForEach-Object { [int]$_.deneme } | Measure-Object -Maximum).Maximum
    $toplam = ($kesinti | ForEach-Object { [int]$_.deneme } | Measure-Object -Sum).Sum
    $tepeSonra = ($sonra | ForEach-Object { [int]$_.deneme } | Measure-Object -Maximum).Maximum
    $hataSonra = ($sonra | ForEach-Object { [int]$_.hata } | Measure-Object -Sum).Sum
    $ttr = @{}
    foreach ($oran in 50, 95, 100) {
        $esik = [math]::Ceiling($n * $oran / 100)
        $x = $r | Where-Object { [int]$_.saniye -ge $basla -and [int]$_.bagli -ge $esik } | Select-Object -First 1
        $ttr[$oran] = if ($x) { [math]::Round([int]$x.saniye - $basla, 1).ToString("0.0", $ci) } else { "yok" }
    }
    $cpu = "-"
    $bcsv = Join-Path $klasor "$($k.kosu).broker.csv"
    if (Test-Path $bcsv) {
        $m = Import-Csv $bcsv | ForEach-Object { [double]::Parse($_.cpu_yuzde, $ci) } | Measure-Object -Maximum
        $cpu = $m.Maximum.ToString("0", $ci)
    }
    "{0,-32} {1,6} {2,6} {3,7} {4,11} {5,10} {6,10} {7,10} {8,6} {9,6} {10,6} {11,12}" -f $k.kosu, $n, "t$kopma", $kapali.ToString("0.0", $ci), $tepe, $tepeSonra, $hataSonra, $toplam, $ttr[50], $ttr[95], $ttr[100], $cpu
}
