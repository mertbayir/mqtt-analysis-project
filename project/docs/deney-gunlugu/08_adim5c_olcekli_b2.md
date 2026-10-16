# 08 — Adım 5c: N'ye göre ölçeklenmiş B2 (N = 10.000)

Tarih: 16 Ekim 2026
Durum: **Matris tamamlandı (9/9 koşu, sürüm v8)**. Hipotezler sonuçlar görülmeden önce yazıldı
Rapor bölümü: Bulgular (5.7 B2 parametre duyarlılığı), Tartışma
Önceki adım: `07_adim5b_n10000.md` (N = 10.000'de B2 (30 sn / 200 ms) tepeyi düşürmedi)

## Amaç

07'de B2 (rastgele gecikme penceresi 30 sn, boşaltma aralığı 200 ms), N = 5.000'de işe yaramış ama N = 10.000'de tepe 7,3 sn'de kalmıştı. Olası açıklama (test edilmemişti): B2'nin parametreleri N ile ölçeklenmedi; 10.000 cihaz pencereye sığınca toplam boşaltma hızı sunucunun taşıyabileceğini aşıyor. Bu adım **iki şeyi** ayırır:

1. **Kontrol (B0):** N = 10.000'de 6 sn'lik tepe, bağlantı sonrası işten mi geliyor, yoksa 10.000 cihazın yeniden bağlanmasının kendisinden mi? (07'de B0 bu ölçekte koşulmamıştı; bu bir boşluktu.)
2. **Ölçeklenmiş B2:** pencere ve aralık büyütülürse (toplam boşaltma hızı düşer) tepe iner mi? Bedeli ne?

## Toplam boşaltma hızının kabaca hesabı (tasarım gerekçesi, ölçüm değil)

N = 10.000'de cihaz başına ortalama birikim ≈ 44 mesaj (07, rastgeleli A: 440 bin mesaj / 10.000 cihaz). Cihaz başına boşaltma süresi D = birikim × aralık; toplam boşaltma hızı ≈ 440 bin / (pencere + D).

| Ayar (pencere / aralık) | D | Ortalama toplam hız |
|---|---|---|
| 30 sn / 200 ms (07'deki B2) | ≈ 9 sn | ≈ 11 bin mesaj/sn (ölçülen tepe ≈ 12 bin) |
| **60 sn / 1000 ms** | ≈ 44 sn | ≈ 4,2 bin mesaj/sn |
| **90 sn / 500 ms** | ≈ 22 sn | ≈ 3,9 bin mesaj/sn |

Canlı yük ayrıca ≈ 6 bin mesaj/sn (10.000 cihaz × 0,6 mesaj/sn). 07'de B1'in ölçülen boşaltma tepesi ≈ 21-23 bin mesaj/sn idi.

## Kurulum

07 ile aynı (N = 10.000, yayılım 20 sn, kesinti 60 sn, tek yönlü saat, aynı kaynak payları), farklar:

- **A = yalnızca rastgeleli üstel** (07'deki en iyi dönüş kuralı).
- **İzleme süresi 180 sn** (önceki 90 sn): uzun pencereli B2'nin boşaltması bitebilsin. Tepe ölçütü sunucu açıldıktan sonraki tüm saniyeler üzerindendir; ilk 90 sn'deki değerler uzun süreden etkilenmez.
- Hücreler: **b0** (kontrol), **b2-w60i1000**, **b2-w90i500** (etiket: `b2-w<pencere>i<aralık>`).
- Karşılaştırma için 07'deki aynı kodla koşulmuş rastgeleli/B1 (6.157 ms) ve rastgeleli/B2 30/200 (7.324 ms) sonuçları kullanılır; bunlar aynı blokta koşulmadı (**sınır**: zaman içinde sapma olabilir).

## Hipotezler (matris öncesi yazıldı)

| # | Hipotez | Not |
|---|---|---|
| H16 | **B0, N = 10.000'de** alarm p99 tepesi 500 ms'nin altında kalır (6 sn'lik tepe bağlantı sonrası işten gelir, yeniden bağlanmanın kendisinden değil) | **Çürüyebilir** |
| H17 | **b2-w60i1000** alarm p99 tepesi 500 ms'nin altında kalır | **Çürüyebilir** |
| H18 | **b2-w90i500** alarm p99 tepesi 500 ms'nin altında kalır | **Çürüyebilir** |
| H19 | Bedel: **b2-w60i1000**'in boşaltma bitişi (sunucu açıldıktan sonra) ≥ 100 sn'dir ve bütün birikim toplayıcıya ulaşır (kayıp ≤ 1) | **Çürüyebilir** |
| H20 | Tüm koşularda tekrar = 0 | **Çürüyebilir** |

Çürürse (özellikle H16 çürürse) bu, sorunun yeniden bağlanmada olduğunu gösterir ve B2'nin ölçeklenmesi anlamsız olur; bu durum olduğu gibi yazılır.

## Tasarım

| Öğe | Değer |
|---|---|
| Hücreler | 3 (b0, b2-w60i1000, b2-w90i500), A = rastgeleli |
| Tekrar | 3 blok (tohum 1-3), blokta rastgele sıra |
| Toplam | 9 koşu, ≈ 55 dk |
| Sürüm | `v8` |
| Komut | `./scripts/matris5.ps1 -Tekrar 3 -Cihazlar 10000 -Politikalar ustel-rastgele -BPolitikalari b0,b2 -B2Varyantlari "60:1000","90:500" -SonrasiSn 180 -Surum v8` |
| Analiz | `python scripts/analiz_adim5.py v8` |

Not: `-BPolitikalari b0,b2` ile `-B2Varyantlari` verildiğinde B2 yalnızca iki varyantla (varsayılan 30:200 yok) koşulur. Betik değişikliği (`adim5.ps1` etiket, `matris5.ps1` varyant ve `-SonrasiSn`) bu adım için eklendi.

## Sonuçlar

### Koşu akışı (kayıt)

Matris `results/matris5_20261016_1525.txt` içinde; 9 koşu, 0 betik hatası. Tüm koşularda tekrar 0.

### Hücre başına sonuçlar (N = 10.000, rastgeleli A, 3 tekrar, medyan [en küçük - en büyük])

Taban (kesinti öncesi alarm p99): 51-54 ms.

| Politika | Alarm p99 tepe (ms) | >100 ms süre (sn) | Boşaltma bitişi (sn) | Boşaltma tepe hızı (msg/sn) | Eşitleme p50 / p99 (ms) | Sunucu CPU tepe (%) | Atılan |
|---|---|---|---|---|---|---|---|
| **B0 (kontrol)** | 90 [82-112] | 0 [0-1] | - | - | - | 37 [36-37] | 441 bin (%27) |
| **B2, 60 sn / 1 sn** | 237 [213-355] | 45 [38-47] | 137,7 [134,9-138,1] | ≈ 7.300 | 24,6 / 143 | 66 [63-67] | 0 |
| **B2, 90 sn / 500 ms** | **169** [154-198] | **21** [13-23] | 140,2 [139,9-140,4] | ≈ 5.100-5.200 | 15,0 / 94 | 60 [59-62] | 0 |

Karşılaştırma (07, aynı kod, farklı blok, 90 sn izleme): B1 tepe 6.157 ms, boşaltma bitişi 34,4 sn, CPU %89; B2 30 sn / 200 ms tepe 7.324 ms, boşaltma bitişi 70,9 sn, CPU %78.

### Hipotezlerin durumu

| # | Sonuç | Kanıt |
|---|---|---|
| H16 | **Destekleniyor** | B0'da tepe 82-112 ms, 100 ms eşiği en fazla 1 sn aşıldı. N = 10.000'de 6 sn'lik tepe yeniden bağlanmanın kendisinden değil, **bağlantı sonrası işten** geliyor |
| H17 | **Destekleniyor** | 60 sn / 1 sn: tepe 213-355 ms (< 500); 30 sn / 200 ms'ye göre %97 düşüş |
| H18 | **Destekleniyor** | 90 sn / 500 ms: tepe 154-198 ms (< 500); %98 düşüş |
| H19 | **Destekleniyor** | 60 sn / 1 sn'de boşaltma bitişi 135-138 sn (≥ 100), 3 koşuda da kayıp 0 |
| H20 | **Destekleniyor** | Tekrar 9 koşunun hepsinde 0 |

### Gözlemler

1. **N = 10.000'deki sorun çözülebiliyor:** B2'nin pencereyi büyütüp aralığı yavaşlatan iki varyantı canlı alarm tepesini 6-7 sn'den 150-350 ms'ye indirdi, sunucu CPU'su %60-66'ya düştü. Eşitleme p99'u 7 sn'den 94-143 ms'ye indi.
2. **Bedel büyük:** boşaltma yaklaşık 140 sn'de bitiyor (B1'de 34 sn, yani ≈ 4 kat). Kesintide biriken veri, kesintinin bitmesinden ~2,3 dk sonra tamamlanıyor.
3. **İki varyant arasında 90 sn / 500 ms daha iyi:** tepe daha düşük (169 ve 237 ms), 100 ms üstü süre yarıdan az (21 ve 45 sn), bitiş süresi hemen hemen aynı (140 ve 138 sn). Pencere ve aralık birlikte değiştiği için hangisinin etkili olduğu bu verilerle ayrılamaz.
4. **Hiçbir B2 varyantı tabana (≈ 52 ms) inmedi.** Tepe 3-7 kat. Tabana en yakın olan B0 (90 ms), mesajların %27'sini atarak sağlıyor.
5. **Ön hesabın yönü doğru çıktı:** tahmin edilen ortalama toplam hız (≈ 4 bin/sn) altında tepe ölçülen hız 5-7 bin/sn oldu (tepe ortalamadan yüksektir); 07'deki ≈ 12 bin/sn ile tepe 7 sn idi. İki noktayla "hız eşiği" tanımlanamaz, yalnızca yön gösterir.

### Sınırlar ve açık noktalar

- **Küçük kayıp devam ediyor:** 90 sn / 500 ms tohum 3'te 2 mesaj kayıp (üretilen 1.619.224, alınan 1.619.222). 06'da 2 koşuda 1'er, 07'de 3 koşuda 1, 91 ve 132 mesaj. Toplamda 39 koşunun 6'sında; neden hâlâ açıklanmadı. Ham kayıt (`RAW_PATH`) ile araştırma gerekli.
- Karşılaştırma hücreleri (B1, B2 30/200) aynı blokta koşulmadı; zaman içi sapma kontrol edilmedi.
- Yalnızca iki varyant ve tek A; ayar uzayı taranmadı, "B2 N ile ölçeklenmeli" iddiası bu iki noktaya dayanır. N = 5.000'de 30/200'ün yeterli, 10.000'de yetersiz olması ile 10.000'de büyük pencerenin yeterli olması birlikte bu yönü destekliyor, bir ölçekleme yasası değil.
- Birikim klinik önceliklendirilmedi: vital ve alarm birikimi aynı sırayla boşalıyor. Alarm birikimini öne almak ayrı bir tasarım (gelecek çalışma).
- 06 ve 07'deki tüm sınırlar geçerli.

## Sınırlar (önceden bilinen)

- Karşılaştırma hücreleri (B1, B2 30/200) farklı zamanda, 90 sn izlemeyle koşuldu.
- Yalnızca iki B2 varyantı; ayar uzayının taranması değil, önceden hesaplanan iki noktanın sınanması.
- Simülatör aynı konteynerde 10.000 cihaz koşturuyor; taban gecikme ≈ 55 ms (07).
- 06 ve 07'deki tüm sınırlar geçerli.
