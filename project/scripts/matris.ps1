# Deney matrisi: kural x cihaz sayısı hücrelerini BLOKLAR hâlinde, her blokta RASTGELE sırayla çalıştırır.
# Blok = bir tekrar. Blok numarası aynı zamanda tohumdur (tohum 1, 2, 3, ...).
# Böylece zamanla değişen etkiler (ısınma, arka plan işleri) hücrelere eşit dağılır (plan: 15a, 14 Bölüm 4).
#
# Kullanım: ./scripts/matris.ps1 -Tekrar 3 -Cihazlar 2000,5000 -Politikalar sabit,ustel,ustel-rastgele
# Sıra ve ilerleme results/matris_<zaman>.txt dosyasına yazılır (hangi koşu hangi sırada yapıldı, kayıt için).
# Bir koşu hata verirse kaydedilir ve sonraki koşuya geçilir (hiçbir şey silinmez).
param(
    [int]$Tekrar = 3,
    [int[]]$Cihazlar = @(2000, 5000),
    [string[]]$Politikalar = @("sabit", "ustel", "ustel-rastgele"),
    [string]$Surum = "v5",
    [int]$SonrasiSn = 90,
    [switch]$Tls,   # Adım 6: cihazlar TLS ile bağlanır
    [string]$Not = "Matris koşusu"
)
$proje = Split-Path $PSScriptRoot -Parent
Set-Location $proje
$klasor = Join-Path $proje "results"
$gunluk = Join-Path $klasor ("matris_{0}.txt" -f (Get-Date -Format "yyyyMMdd_HHmm"))
$rastgele = New-Object System.Random   # blok içi sıra için (sıra gunluğe yazılır)

$hucreler = foreach ($p in $Politikalar) { foreach ($n in $Cihazlar) { [pscustomobject]@{ P = $p; N = $n } } }
"Matris başladı: {0}  tekrar={1} cihazlar={2} kurallar={3} sürüm={4} sonrasiSn={5}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"),
    $Tekrar, ($Cihazlar -join ","), ($Politikalar -join ","), $Surum, $SonrasiSn | Tee-Object -FilePath $gunluk

for ($blok = 1; $blok -le $Tekrar; $blok++) {
    $sirali = $hucreler | Sort-Object { $rastgele.Next() }
    "--- Blok $blok (tohum $blok), sıra: " + (($sirali | ForEach-Object { "$($_.P)/$($_.N)" }) -join "  ") | Tee-Object -FilePath $gunluk -Append
    foreach ($h in $sirali) {
        $ad = "adim3_$($h.P)_n$($h.N)_s${blok}_${Surum}"
        "{0}  BAŞLA {1}" -f (Get-Date -Format "HH:mm:ss"), $ad | Tee-Object -FilePath $gunluk -Append
        try {
            ./scripts/adim3.ps1 -Tls:$Tls -Politika $h.P -Cihaz $h.N -Tohum $blok -Surum $Surum -SonrasiSn $SonrasiSn -Not "$Not (blok $blok)" | Out-Null
            "{0}  BİTTİ {1}" -f (Get-Date -Format "HH:mm:ss"), $ad | Tee-Object -FilePath $gunluk -Append
        } catch {
            "{0}  HATA  {1}: {2}" -f (Get-Date -Format "HH:mm:ss"), $ad, $_.Exception.Message | Tee-Object -FilePath $gunluk -Append
        }
    }
}
"Matris bitti: {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss") | Tee-Object -FilePath $gunluk -Append
