# 08 — Adım 5c: N'ye göre ölçeklenmiş B2 (N = 10.000)

Tarih: 16 Ekim 2026
Durum: **Matris başlıyor** (hipotezler sonuçlar görülmeden önce yazıldı)
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

`[ ]` Matris bitince doldurulacak.

## Sınırlar (önceden bilinen)

- Karşılaştırma hücreleri (B1, B2 30/200) farklı zamanda, 90 sn izlemeyle koşuldu.
- Yalnızca iki B2 varyantı; ayar uzayının taranması değil, önceden hesaplanan iki noktanın sınanması.
- Simülatör aynı konteynerde 10.000 cihaz koşturuyor; taban gecikme ≈ 55 ms (07).
- 06 ve 07'deki tüm sınırlar geçerli.
