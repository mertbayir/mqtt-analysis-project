# 10 — Adım 6: TLS ile toplu yeniden bağlanma

Tarih: 16 Ekim 2026
Durum: **Matris başlıyor** (hipotezler sonuçlar görülmeden önce yazıldı; ölçek pilotunun sonucu yazım sırasında biliniyordu, bkz. "Pilot")
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

`[ ]` Matris bitince doldurulacak.

## Sınırlar (önceden bilinen)

- Sertifika doğrulanmaz; TLS sürümü ve şifre takımı ölçülmedi; oturum devam ettirme etkisi bilinmiyor.
- İstemci tarafı el sıkışma maliyeti ayrılamıyor (simülatör 4 CPU / 3 GB).
- TLS'siz karşılaştırma Adım 4'ten, aynı blokta değil.
- Adım 4'ün tüm sınırları geçerli (tek makine, tek sunucu, temiz tek kesinti, sanal cihaz).
