# Adım 2 koşularının özet tablosunu üretir (makaledeki sayılar buradan gelir, elle hesaplanmaz).
# Kullanım: ./scripts/ozet.ps1            -> ekrana yazar
#           ./scripts/ozet.ps1 | Out-File results/ozet_adim2.txt -Encoding utf8
#
# Tanımlar:
#  kopma_t        : bağlı cihaz sayısının ilk kez 0 olduğu saniye
#  kesinti        : kopma_t ile sunucunun yeniden başlatıldığı an (kapali_sn sonra) arası
#  tepe_deneme    : kesintide bir saniyede yapılan en çok bağlantı denemesi
#  toplam_deneme  : kesintide toplam deneme
#  TTR %50/95/100 : sunucu başlatıldıktan sonra bağlı cihaz oranının ilk kez %50/%95/%100'e ulaştığı süre (sn, 1 sn çözünürlük)
$ci = [Globalization.CultureInfo]::InvariantCulture
$klasor = Join-Path (Split-Path $PSScriptRoot -Parent) "results"
$kayitlar = Import-Csv (Join-Path $klasor "deney_kayitlari.csv")

"{0,-30} {1,5} {2,6} {3,8} {4,12} {5,13} {6,6} {7,6} {8,6}" -f "kosu","cihaz","kopma","kapali","tepe_deneme","toplam_deneme","TTR50","TTR95","TTR100"
foreach ($k in $kayitlar) {
    $csv = Join-Path $klasor "$($k.kosu).csv"
    if (-not (Test-Path $csv)) { continue }
    $r = Import-Csv $csv
    $n = [int]$k.cihaz
    $kapali = [double]::Parse($k.kapali_sn, $ci)
    $kopma = [int](($r | Where-Object { [int]$_.bagli -eq 0 } | Select-Object -First 1).saniye)
    $basla = $kopma + $kapali
    $kesinti = $r | Where-Object { [int]$_.saniye -ge $kopma -and [int]$_.saniye -le $basla }
    $tepe = ($kesinti | ForEach-Object { [int]$_.deneme } | Measure-Object -Maximum).Maximum
    $toplam = ($kesinti | ForEach-Object { [int]$_.deneme } | Measure-Object -Sum).Sum
    $ttr = @{}
    foreach ($oran in 50, 95, 100) {
        $esik = [math]::Ceiling($n * $oran / 100)
        $x = $r | Where-Object { [int]$_.saniye -ge $basla -and [int]$_.bagli -ge $esik } | Select-Object -First 1
        $ttr[$oran] = if ($x) { [math]::Round([int]$x.saniye - $basla, 1).ToString("0.0", $ci) } else { "yok" }
    }
    "{0,-30} {1,5} {2,6} {3,8} {4,12} {5,13} {6,6} {7,6} {8,6}" -f $k.kosu, $n, "t$kopma", $kapali.ToString("0.0", $ci), $tepe, $toplam, $ttr[50], $ttr[95], $ttr[100]
}
