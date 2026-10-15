# Adım 5 matrisi: A (yeniden bağlanma) x B (bağlantı sonrası) hücrelerini BLOKLAR hâlinde, her blokta RASTGELE sırayla çalıştırır.
# Blok = bir tekrar; blok numarası = tohum. Sıra ve ilerleme results/matris5_<zaman>.txt dosyasına yazılır.
# Kullanım: ./scripts/matris5.ps1 -Tekrar 3 -Cihazlar 5000 -Surum v6
param(
    [int]$Tekrar = 3,
    [int[]]$Cihazlar = @(5000),
    [string[]]$Politikalar = @("ustel", "ustel-rastgele"),
    [string[]]$BPolitikalari = @("b0", "b1", "b2"),
    [string]$Surum = "v6",
    [string]$Not = "Adim 5 matris"
)
$proje = Split-Path $PSScriptRoot -Parent
Set-Location $proje
$gunluk = Join-Path (Join-Path $proje "results") ("matris5_{0}.txt" -f (Get-Date -Format "yyyyMMdd_HHmm"))
$rastgele = New-Object System.Random

$hucreler = foreach ($a in $Politikalar) { foreach ($b in $BPolitikalari) { foreach ($n in $Cihazlar) { [pscustomobject]@{ A = $a; B = $b; N = $n } } } }
"Matris5 başladı: {0}  tekrar={1} cihazlar={2} A={3} B={4} sürüm={5}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"),
    $Tekrar, ($Cihazlar -join ","), ($Politikalar -join ","), ($BPolitikalari -join ","), $Surum | Tee-Object -FilePath $gunluk

for ($blok = 1; $blok -le $Tekrar; $blok++) {
    $sirali = $hucreler | Sort-Object { $rastgele.Next() }
    "--- Blok $blok (tohum $blok), sıra: " + (($sirali | ForEach-Object { "$($_.A)/$($_.B)/$($_.N)" }) -join "  ") | Tee-Object -FilePath $gunluk -Append
    foreach ($h in $sirali) {
        $ad = "adim5_$($h.A)_$($h.B)_n$($h.N)_s${blok}_${Surum}"
        "{0}  BAŞLA {1}" -f (Get-Date -Format "HH:mm:ss"), $ad | Tee-Object -FilePath $gunluk -Append
        try {
            ./scripts/adim5.ps1 -A $h.A -B $h.B -Cihaz $h.N -Tohum $blok -Surum $Surum -Not "$Not (blok $blok)" | Out-Null
            "{0}  BİTTİ {1}" -f (Get-Date -Format "HH:mm:ss"), $ad | Tee-Object -FilePath $gunluk -Append
        } catch {
            "{0}  HATA  {1}: {2}" -f (Get-Date -Format "HH:mm:ss"), $ad, $_.Exception.Message | Tee-Object -FilePath $gunluk -Append
        }
    }
}
"Matris5 bitti: {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss") | Tee-Object -FilePath $gunluk -Append
