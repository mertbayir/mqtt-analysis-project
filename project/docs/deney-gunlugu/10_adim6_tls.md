# 10 — Adım 6: TLS ile toplu yeniden bağlanma

Tarih: 16 Ekim 2026
Durum: **Matris tamamlandı (18/18 koşu, sürüm t1)**. Hipotezler sonuçlar görülmeden önce yazıldı; ölçek pilotunun sonucu yazım sırasında biliniyordu, bkz. "Pilot")
Rapor bölümü: Bulgular (5.4), Tartışma
Önceki adım: `05_adim4_matris_v5.md` (TLS'siz; sunucu zorlanmadı)

## Amaç

Adım 4'te TLS'siz Mosquitto 5.000 cihazın saniyedeki yeniden bağlanmasını ilk denemede karşıladı (CPU en çok %30); bu yüzden dönüşü dağıtmanın (rastgeleli kural) faydası ölçülemedi, yalnızca bedeli ölçüldü. Gerçek sistemler TLS kullanır ve TLS el sıkışması (asimetrik şifreleme) tek çekirdekli sunucuda bağlantı başına çok daha pahalıdır. Soru: TLS ile bağlanma aşaması sunucuyu **zorluyor mu**, ve zorluyorsa rastgeleli kuralın faydası **görünür** oluyor mu?

## Kurulum

Adım 4 (v5) ile aynı tasarım ve betikler (`scripts/matris.ps1` + `adim3.ps1`, `-Tls`), tek fark cihazların sunucuya **TLS ile (port 8883)** bağlanması:

| Öğe | Değer |
|---|---|
| Sunucu | Mosquitto 2.1.2, 2 CPU / 2 GB; ek dinleyici 8883 (TLS), 1883 (TLS'siz) kalır |
| Sertifika | Atılabilir test CA + sunucu sertifikası, RSA-2048 (`scripts/tls_sertifika.sh`, anahtarlar git dışı) |
| İstemci | MQTTnet TLS; **sertifika doğrulanmaz** (test sertifikası, yük testi): el sıkışmanın asimetrik işlem maliyeti aynıdır, zincir doğrulama maliyeti dışarıda kalır |
| Protokol sürümü | TLS sürümü ve şifre takımı **işletim sistemi/OpenSSL varsayılanıdır, ayrıca ölçülmedi** |
| Oturum devam ettirme | Ayrıca denenmedi/ölçülmedi (bilinmiyor) |
| İş yükü | Yok (yalnızca bağlanma); bağlantı sonrası iş TLS'te denenmedi |
| Kesinti | Temiz, 60 sn kapalı; ilk bağlanma 20 sn'ye yayılı, kesinti t = 40 sn; **izleme 150 sn** (v5'te 90 sn: TLS'te toparlanma uzayabileceği için) |
| Karşılaştırma | Aynı koşulların TLS'siz hali Adım 4'te (v5). Aynı blokta koşulmadı (**sınır**) |

## Pilot (nihai veri değil)

İlk TLS koşuları: 200 cihaz (çalışma testi, bütün cihazlar bağlandı, `adim3_ustel_n200_s1_tlstest`) ve **N = 5.000, üstel, tek koşu** (`adim3_ustel_n5000_s1_tpilot`):

| Ölçüt | v5, TLS'siz (üstel, N = 5.000, medyan) | tpilot, TLS (tek koşu) |
|---|---|---|
| TTR %50 | 24,1 sn | 27,8 sn |
| TTR %100 | 25,1 sn | **62,8 sn** |
| Sunucu açıkken başarısız bağlantı | 0 | **984** |
| Sunucu CPU tepe | 12,2 | **63,7** |
| Simülatör CPU tepe | 176 | 257 |

TLS ile sunucu bağlanma aşamasında zorlanmaya başladı (ilk kez başarısız bağlantı ve uzamış toparlanma). Simülatörün de CPU'su yükseldi: **el sıkışma maliyetinin ne kadarının istemci tarafında olduğu ayrılamıyor** (sınır).

## Hipotezler (matris öncesi yazıldı)

| # | Hipotez | Not |
|---|---|---|
| HT1 | **Üstel kural, N = 5.000, TLS:** TTR %100 medyanı TLS'siz değerin (25,1 sn) en az 2 katıdır | Pilotta gözlendi (62,8 sn); tekrarlı doğrulama |
| HT2 | **Üstel kural, N = 5.000, TLS:** sunucu açıkken başarısız bağlantı sayısı > 0 (TLS'siz 0) | Pilotta gözlendi |
| HT3 | **TLS'te rastgeleli üstel kural, üstel kurala göre TTR %100'ü kısaltır** (Adım 4'te TLS'siz uzatıyordu: 34-35 sn'ye karşı 25 sn) | **Çürüyebilir**, çalışmanın asıl sorusu |
| HT4 | TLS ile sunucu CPU tepesi, aynı kural ve N için TLS'siz değerin en az 3 katıdır (3 kuralın hepsinde, N = 5.000) | **Çürüyebilir** |
| HT5 | **Devrilme noktası N = 2.000 ile 5.000 arasındadır:** N = 2.000'de üstel kural TLS'te TTR %100 en çok 1,5 × TLS'siz değerdir (≤ 38 sn); N = 5.000'de HT1 geçerlidir | **Çürüyebilir** |
| HT6 | **Sabit kural TLS'te** TLS'siz değerden belirgin kötüdür: TTR %100 ≥ 2 × TLS'siz (6-7 sn) (zaman aşımı yeniden deneme yığılması) | **Çürüyebilir** |

## Tasarım

| Öğe | Değer |
|---|---|
| Faktörler | kural (sabit, üstel, rastgeleli üstel) × N (2.000, 5.000) = 6 hücre |
| Tekrar | 3 blok (tohum 1-3), blokta rastgele sıra; 18 koşu |
| Sürüm | `t1` (koşu adı `adim3_<kural>_n<N>_s<tohum>_t1`) |
| Komut | `./scripts/matris.ps1 -Tekrar 3 -Tls -Surum t1 -SonrasiSn 150 -Not "Adim 6 TLS"` |
| Analiz | `python scripts/tekrar_ozet.py t1` |
| Süre | ≈ 18 × 4,5 dk ≈ 80 dk |

## Sonuçlar

### Koşu akışı (kayıt)

Matris günlüğü `results/matris_20261016_1835.txt`; 18 koşu, 0 betik hatası. Koşu başına ve hücre özetleri: `results/ozet_adim4_t1_kosular.csv`, `ozet_adim4_t1_hucreler.csv` (`scripts/tekrar_ozet.py t1`). Not: betik ilk çalıştırmada v5 dosyalarının (`ozet_adim4_kosular.csv`, `_hucreler.csv`) üzerine yazdı; git'ten geri yüklendi ve betik sürüm etiketli dosya adı yazacak şekilde düzeltildi.

### Hücre başına sonuçlar (TLS; medyan [en küçük-en büyük]) ve TLS'siz (v5) medyan

| N | Kural | TTR %50 (sn) | TTR %100 (sn) | Sunucu açıkken başarısız bağlantı | Sunucu CPU tepe |
|---|---|---|---|---|---|
| 2.000 | sabit | 2,1 [1,2-2,2] (v5: 5,3) | 3,2 [3,1-3,2] (6,3) | 670 [64-700] (2.065) | 78,4 [70,7-79,5] (9,9) |
| 2.000 | üstel | 26,2 [26,1-27,1] (24,2) | **61,2** [60,1-63,1] (25,2) | 325 [105-571] (0) | 33,9 [19,8-36,8] (3,3) |
| 2.000 | rastgeleli üstel | 12,0 [11,2-12,1] (12,3) | 28,1 [27,2-30,0] (34,3) | 0 [0-26] (243) | 13,0 [12,5-13,6] (1,8) |
| 5.000 | sabit | 3,9 [3,0-4,0] (5,1) | 9,9 [8,0-11,0] (7,1) | 1.128 [321-1.780] (5.098) | 85,4 [82,0-89,0] (24,2) |
| 5.000 | üstel | 28,0 [26,0-28,9] (24,1) | **62,9** [62,0-95,0] (25,1) | **1.836** [1.119-1.919] (0) | 63,7 [63,5-89,8] (12,2) |
| 5.000 | rastgeleli üstel | 10,9 [10,9-11,9] (12,1) | 28,9 [28,9-29,9] (35,1) | 0 [0-0] (576) | 30,0 [29,7-31,9] (6,8) |

Kesintideki tepe deneme/sn TLS'siz değerlerle aynı (sabit ve üstelde N; rastgelelide N'nin ≈ %25-29'u).

### Hipotezlerin durumu

| # | Sonuç | Kanıt |
|---|---|---|
| HT1 | **Destekleniyor** | Üstel, N = 5.000: TTR %100 62,9 sn (62,0-95,0), TLS'siz 25,1 sn; 2,5 kat |
| HT2 | **Destekleniyor** | Üstel, N = 5.000: sunucu açıkken başarısız bağlantı 1.119-1.919 (3 koşunun hepsinde > 1.000; TLS'siz 0) |
| HT3 | **Destekleniyor** | TLS'te rastgeleli kural TTR %100'ü yarıdan fazla kısaltır: N = 5.000'de 28,9 sn (üstel 62,9), N = 2.000'de 28,1 sn (üstel 61,2). TLS'siz durumda yön tersti (rastgeleli 34-35 sn, üstel 25 sn); **yön TLS ile ters döndü** |
| HT4 | **Destekleniyor** | N = 5.000'de sunucu CPU tepesi TLS/TLS'siz: sabit 3,5 kat, üstel 5,2 kat, rastgeleli 4,4 kat |
| HT5 | **Çürüdü** | N = 2.000'de üstel kural TLS'te TTR %100 61,2 sn (≤ 38 sn bekleniyordu, TLS'siz 25,2); etki N = 2.000'de de var, ve başarısız bağlantı 105-571. Devrilme noktası N = 2.000'in altında (ya da yok) |
| HT6 | **Çürüdü** | Sabit kural TLS'te TLS'siz değerden kötü değil: N = 2.000'de 3,2 sn (TLS'siz 6,3), N = 5.000'de 9,9 sn (7,1; yalnızca 1,4 kat) |

### Gözlemler

1. **TLS bağlanma aşamasını gerçek bir yük hâline getiriyor.** Aynı kural ve N için sunucu CPU tepesi 3,5-5,2 kat yükseliyor (üstel/N = 5.000'de TLS'siz %12 → %64); üstel kuralda ilk kez sunucu açıkken başarısız bağlantılar görülüyor (N = 5.000: 1.119-1.919; TLS'siz 0).
2. **Üstel kuralın TLS'teki TTR'si yine bir takvim sonucu.** TTR %100 üç tohumda 60-63 sn (N = 2.000) ve 62-63 sn (N = 5.000; biri 95 sn). Hesap: kopmadan sonra denemeler t = 1, 8, 17, 30, 51, 86 sn'de; sunucu t ≈ 60,9 sn'de açılıyor; 86. saniyedeki ilk dalganın el sıkışması sunucuyu aşınca zaman aşımına uğrayanlar 5 sn sonra, 30 sn'lik üst sınır beklemesiyle t = 121 sn'de dener: 121 − 60,9 = 60,1 sn. Bir koşuda (N = 5.000, tohum 3) ikinci dalga da yetmeyip üçüncü dalga t = 156 sn'de gerçekleşir: 156 − 60,9 = 95,1 sn; ölçülen 95,0 sn. Sayısal örtüşme güçlü, ama mekanizma (ikinci dalganın zaman aşımı yüzünden olduğu) doğrudan gözlenmedi, hesapla tutarlı.
3. **Rastgeleli kuralın faydası ancak sunucu zorlanınca görünüyor.** TLS'siz ortamda yalnızca bedeli ölçülüyordu (TTR %100 +9-10 sn); TLS'te kural hem toparlanmayı kısaltıyor (≈ 62 → ≈ 29 sn) hem sunucu açıkken başarısız bağlantıyı sıfırlıyor (N = 5.000'de 0) hem de sunucu CPU'sunu yarıya indiriyor (63,7 → 30,0). Adım 4'ün "faydası ölçülemedi" sınırının cevabı budur.
4. **Sabit kural TLS'te sunucuyu en çok yoran kural:** N = 5.000'de CPU tepesi 82-89 (tek çekirdeğin ≈ %85'i). TTR yine kısa (9,9 sn) çünkü yeniden deneme sıklığı yüksek.
5. **Sabit kuralın N = 2.000'de TLS'siz değerden hızlı çıkması (3,2 sn'ye karşı 6,3 sn) açıklanmadı;** faz etkisi olabilir (Adım 4'te de sabit kuralda faz etkisi görülmüştü), ölçülmedi.

### Sınırlar ve açık noktalar

- TLS'siz karşılaştırma Adım 4'ten, aynı blokta değil; ayrıca Adım 4'te izleme 90 sn idi, burada 150 sn (TTR bu pencerede tamamlandı, ölçüm sınırı aşılmadı).
- İstemci tarafı el sıkışma maliyeti ayrılamıyor; simülatör CPU tepesi en çok 320 (sınır 400; üstel/N = 5.000, tohum 1), yani yük üreticisi bazı koşularda sınırın ≈ %80'ine dayandı; TLS'te gecikmenin bir kısmı istemci tarafından olabilir.
- Sertifika doğrulanmadı; TLS sürümü ve şifre takımı ölçülmedi; oturum devam ettirmenin etkisi bilinmiyor.
- Yalnızca bağlanma aşaması ve TLS'li; bağlantı sonrası iş (Adım 5) TLS'te denenmedi.
- Hücre başına 3 koşu; üstel/N = 5.000'de tek koşu 95 sn ile aykırı görünür, çıkarılmadı.

## Sınırlar (önceden bilinen)

- Sertifika doğrulanmaz; TLS sürümü ve şifre takımı ölçülmedi; oturum devam ettirme etkisi bilinmiyor.
- İstemci tarafı el sıkışma maliyeti ayrılamıyor (simülatör 4 CPU / 3 GB).
- TLS'siz karşılaştırma Adım 4'ten, aynı blokta değil.
- Adım 4'ün tüm sınırları geçerli (tek makine, tek sunucu, temiz tek kesinti, sanal cihaz).
