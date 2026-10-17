# Adım 5 matrisi: A (yeniden bağlanma) x B (bağlantı sonrası) hücrelerini BLOKLAR hâlinde, her blokta RASTGELE sırayla çalıştırır.
# Blok = bir tekrar; blok numarası = tohum. Sıra ve ilerleme results/matris5_<zaman>.txt dosyasına yazılır.
# Kullanım: ./scripts/matris5.ps1 -Tekrar 3 -Cihazlar 5000 -Surum v6
param(
    [int]$Tekrar = 3,
    [int[]]$Cihazlar = @(5000),
    [string[]]$Politikalar = @("ustel", "ustel-rastgele"),
    [string[]]$BPolitikalari = @("b0", "b1", "b2"),
    [string[]]$B2Varyantlari = @(),   # "pencereSn:aralikMs" listesi, örn. "60:1000","90:500"; boşsa B2 varsayılan (30:200)
    [int]$SonrasiSn = 90,
    [switch]$HamKayit,
    [ValidateSet("mosquitto", "emqx")] [string]$Sunucu = "mosquitto",
    [int]$SunucuCpu = 2,
    [switch]$Tls,   # Adım 6b: cihazlar TLS ile bağlanır (toplayıcı TLS'siz)
    [string]$Surum = "v6",
    [string]$Not = "Adim 5 matris",
    [switch]$TamamlananiAtla   # kayıt defterinde zaten olan koşuları atlar (yarıda kesilen matrisi sürdürmek için)
)
$proje = Split-Path $PSScriptRoot -Parent
Set-Location $proje
$gunluk = Join-Path (Join-Path $proje "results") ("matris5_{0}.txt" -f (Get-Date -Format "yyyyMMdd_HHmm"))
$rastgele = New-Object System.Random

$hucreler = foreach ($a in $Politikalar) { foreach ($b in $BPolitikalari) { foreach ($n in $Cihazlar) {
    if ($b -eq "b2" -and $B2Varyantlari.Count -gt 0) {
        foreach ($v in $B2Varyantlari) { $pa = $v.Split(":"); [pscustomobject]@{ A = $a; B = $b; N = $n; W = [int]$pa[0]; I = [int]$pa[1] } }
    } else { [pscustomobject]@{ A = $a; B = $b; N = $n; W = 30; I = 200 } }
} } }
"Matris5 başladı: {0}  tekrar={1} cihazlar={2} A={3} B={4} sürüm={5}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"),
    $Tekrar, ($Cihazlar -join ","), ($Politikalar -join ","), ($BPolitikalari -join ","), $Surum | Tee-Object -FilePath $gunluk

for ($blok = 1; $blok -le $Tekrar; $blok++) {
    $sirali = $hucreler | Sort-Object { $rastgele.Next() }
    "--- Blok $blok (tohum $blok), sıra: " + (($sirali | ForEach-Object { "$($_.A)/$($_.B)$(if($_.B -eq 'b2' -and ($_.W -ne 30 -or $_.I -ne 200)){"-w$($_.W)i$($_.I)"})/$($_.N)" }) -join "  ") | Tee-Object -FilePath $gunluk -Append
    foreach ($h in $sirali) {
        $be = if ($h.B -eq "b2" -and ($h.W -ne 30 -or $h.I -ne 200)) { "b2-w$($h.W)i$($h.I)" } else { $h.B }
        $ad = "adim5_$($h.A)_${be}_n$($h.N)_s${blok}_${Surum}"
        if ($TamamlananiAtla -and (Select-String -Path (Join-Path $proje "results\deney_kayitlari.csv") -Pattern ("^" + [regex]::Escape($ad) + ",") -Quiet)) {
            "{0}  ATLANDI (zaten tamam) {1}" -f (Get-Date -Format "HH:mm:ss"), $ad | Tee-Object -FilePath $gunluk -Append
            continue
        }
        "{0}  BAŞLA {1}" -f (Get-Date -Format "HH:mm:ss"), $ad | Tee-Object -FilePath $gunluk -Append
        try {
            ./scripts/adim5.ps1 -Sunucu $Sunucu -SunucuCpu $SunucuCpu -Tls:$Tls -HamKayit:$HamKayit -A $h.A -B $h.B -Cihaz $h.N -Tohum $blok -SonrasiSn $SonrasiSn -B2PencereSn $h.W -B2AralikMs $h.I -Surum $Surum -Not "$Not (blok $blok)" | Out-Null
            "{0}  BİTTİ {1}" -f (Get-Date -Format "HH:mm:ss"), $ad | Tee-Object -FilePath $gunluk -Append
        } catch {
            "{0}  HATA  {1}: {2}" -f (Get-Date -Format "HH:mm:ss"), $ad, $_.Exception.Message | Tee-Object -FilePath $gunluk -Append
        }
    }
}
"Matris5 bitti: {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss") | Tee-Object -FilePath $gunluk -Append
