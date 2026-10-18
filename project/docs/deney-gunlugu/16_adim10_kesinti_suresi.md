# 16 — Adım 10: Kesinti süresi taraması

Tarih: 18 Ekim 2026. Durum: **tasarım ve hipotezler yazıldı, koşular başlamadan önce** (sonuçlar bu dosyanın altına eklenecek).

## Soru

Şu ana kadar bütün adımlarda sunucu **60 sn** kapalı kaldı. Kapalı kalma süresi değişirse hem biriken mesaj miktarı (süreyle orantılı) hem de üstel beklemenin hangi aşamada olduğu (1, 2, 4, 8, 16, 30 sn; kümülatif 1, 3, 7, 15, 31, 61, 91, 121, 151, 181 sn) değişir. Bulgularımız yalnızca 60 sn için mi geçerli?

## Tasarım

| Öğe | Değer |
|---|---|
| Sunucu | Mosquitto 2.1.2, 2 CPU, TLS'siz, kesinti türü temiz (`docker stop/start`) |
| Cihaz | 5.000 |
| Bekleme kuralı | üstel, rastgeleli üstel |
| Bağlantı sonrası | B1 (hepsini hemen boşalt) |
| Kesinti süresi | 15, 60, 180 sn |
| Zaman çizelgesi | 40 sn öncesi, kesinti, 100 sn sonrası |
| Tekrar | 4 blok (blok no = tohum), blok içinde rastgele sıra |
| Hücre sayısı | 2 × 3 = 6; toplam 24 koşu |
| Sürüm etiketi | `k10`; 60 sn dışındakilerin adı `-k15`, `-k180` eki alır |
| Tahmini süre | ≈ 2,5 saat |

60 sn hücresi bu taramada **yeniden** ölçülür (adım 5'in 60 sn hücresi başka blokta; bloklar arası değişkenlik büyük olduğu için karşılaştırma aynı blok içinde yapılır).

## Modelin tahmini (ölçümden ÖNCE, 18 Ekim 2026)

Web gezgininin kalibre modeli (Mosquitto parametreleri, 5 tohum medyanı). Model bu kesinti sürelerinde **hiç kalibre edilmedi** (yalnızca 60 sn hücreleriyle), yani gerçek bir ön-kayıtlı sınav.

| Kural | Kesinti | Tepe (ms) | >100 ms süre (sn) |
|---|---|---|---|
| üstel | 15 | 9.255 | 4 |
| üstel | 60 | 9.255 | 12 |
| üstel | 180 | 12.530 | 30 |
| rastgeleli üstel | 15 | 395 | 5 |
| rastgeleli üstel | 60 | 223 | 9 |
| rastgeleli üstel | 180 | 832 | 31 |

Model, üstel kuralda tepenin kesinti süresiyle **orantılı artmadığını** (9,3 sn'de doyduğunu) öngörüyor. Benim ilk sezgim ise biriken mesaj miktarı süreyle orantılı olduğu için tepenin de artması yönündeydi. Deney ikisini ayırt eder.

## Hipotezler (sonuçları görmeden)

Ölçüt: 4 bloğun medyanı; "tepe" = kesinti sonrası canlı alarm p99'un en büyük saniyelik değeri.

- **HK1.** Üstel + B1'de tepe kesinti süresiyle artar: tepe(15) ≤ 0,7 × tepe(60) **ve** tepe(180) ≥ 1,5 × tepe(60). (Model karşı görüşte: oranlar ≈ 1,0 ve ≈ 1,35.)
- **HK2.** Rastgeleli + B1'de tepe 15 ve 60 sn'de 300 ms'nin altında kalır, 180 sn'de 300 ms'yi aşar (biriken iş 30 sn'ye yayılsa da sunucu kapasitesine yaklaşır).
- **HK3.** Her kesinti süresinde rastgeleli, üstelden en az 5 kat düşük tepe verir.
- **HK4.** Kayıp ≤ 1 ve tekrar = 0 (tüm 24 koşuda).
- **HK5 (modelin sınavı).** Model, 6 hücrenin en az 4'ünde tepeyi 2 kat içinde bulur. Bu, daha önce kullandığımız kabul ölçütünün (tepe 2 kat) bu hücrelere uygulanmasıdır. Tutmayan hücreler açıkça yazılır.

Not: 15 sn'de üstel kuralın denemeleri kümülatif 15. sn'ye denk gelir; sunucunun açılış anına çok yakın olduğu için bu hücrede bekleme dışı sapma görülebilir. Bu, hipotez değil gözlem olarak izlenecek.

## Koşu komutu

```
./scripts/matris5.ps1 -Tekrar 4 -Cihazlar 5000 -BPolitikalari b1 -KesintiSureleri 15,60,180 -SonrasiSn 100 -Surum k10 -Not "Adim 10 kesinti suresi"
```

## Sonuçlar

Koşu: 18 Ekim 2026, 19:55-21:44 (1 saat 49 dk); 24 koşu, betik hatası 0; tekrar 24 koşuda 0. Analiz: `python scripts/analiz_adim5.py k10`; dosyalar `results/adim5_*_k10.*`, `results/ozet_adim5_k10_kosular.csv`, `..._hucreler.csv`. Taban 26,5-35 ms (tüm koşularda). Sürüm etiketi `k10`.

### Ölçümler (medyan [en küçük-en büyük], 4 tekrar)

| Kural | Kesinti | Alarm p99 tepe (ms) | >100 ms süre (sn) | Boşaltma bitişi (sn) | Sync p99 (ms) | Sunucu CPU tepe (%) |
|---|---|---|---|---|---|---|
| üstel | 15 | **1.948** [1.575-2.193] | 6 [4-8] | 5,9 [4,7-9,9] | 1.948 [1.565-2.125] | 83 [63-90] |
| üstel | 60 | **9.066** [8.634-9.870] | 19,5 [18-21] | 42,6 [41,8-43,6] | 8.107 [7.867-8.873] | 98 [96-99] |
| üstel | 180 | **20.033** [19.438-21.030] | 38,5 [37-40] | 44,7 [43,3-45,1] | 7.580 [7.146-9.883] | 100 [99-102] |
| rastgeleli | 15 | **242** [157-433] | 2 [1-4] | 9,0 [8,0-11,1] | 178 [24-427] | 83 [66-89] |
| rastgeleli | 60 | **49** [42-53] | 0 [0-0] | 29,2 [28,2-29,8] | 9 [5-10] | 84 [83-86] |
| rastgeleli | 180 | **1.529** [1.220-2.386] | 22 [10-27] | 30,2 [29,7-30,6] | 1.503 [1.188-2.361] | 96 [95-97] |

Kayıp (üretilen − alınan): 24 koşunun 20'sinde 0. Sıfırdan farklı olanlar: rastgeleli 15 sn tohum 1 → **255**, tohum 2 → **314** (cihaz tarafı eşitleme zaman aşımı sayısı o iki koşuda 1.214 ve 1.408; aynı hücrenin öteki iki koşusunda 0); rastgeleli 180 sn tohum 3 → 2; üstel 15 sn tohum 3 → 1. Ham kayıt kapalıydı, bu yüzden kaybın nedeni araştırılamadı.

### Hipotezlerin durumu

| # | Sonuç | Kanıt |
|---|---|---|
| HK1 | **Destekleniyor** | tepe(15)/tepe(60) = 0,21 (≤ 0,7 gerekli); tepe(180)/tepe(60) = 2,21 (≥ 1,5 gerekli). Modelin öngördüğü doyma (≈ 1,0 ve ≈ 1,35) gözlenmedi: üstelde tepe kesinti süresiyle artıyor |
| HK2 | **Destekleniyor** | medyan 15 sn'de 242 ms, 60 sn'de 49 ms (< 300); 180 sn'de 1.529 ms (> 300). Not: 15 sn'nin 4 koşusundan biri 300 ms'yi aştı (433) |
| HK3 | **Destekleniyor** | üstel / rastgeleli tepe oranı: 15 sn'de 8,0×, 60 sn'de 185×, 180 sn'de 13,1× (hepsi ≥ 5×). Fark 60 sn'de en büyük |
| HK4 | **Çürüdü (kayıp)** | Kayıp ≤ 1 beklenmişti; 4 koşuda kayıp var (255, 314, 2, 1). Tekrar 24 koşuda 0 (bu yarısı sağlandı) |
| HK5 | **Destekleniyor (ucu ucuna, 4/6)** | Model tepesi / ölçüm: üstel 15 sn 9.255 / 1.948 = **4,8×** (tutmadı); üstel 60 sn 1,0×; üstel 180 sn 12.530 / 20.033 = 0,63× (tuttu); rastgeleli 15 sn 395 / 242 = 1,6× (tuttu); rastgeleli 60 sn 223 / 49 = **4,5×** (tutmadı, adım 5'ten beri bilinen sapma); rastgeleli 180 sn 832 / 1.529 = 0,54× (tuttu) |

### Gözlemler (bir kısmı sonradan, hipotez dışı)

1. **Üstel + B1'de tepe, kesinti süresiyle neredeyse doğrusal artıyor:** kesinti saniyesi başına 0,11-0,15 sn (1.948 / 16, 9.066 / 61, 20.033 / 181 ms). Basit bir hesapla uyumlu: cihazlar toplam 3.000 mesaj/sn üretiyor (5.000 × 0,6; iki bloktaki üretilen sayıların farkından doğrulandı: 360 bin / 120 sn), kesintide biriken ≈ 3.000 × süre; sunucu tepe hızı 20-27 bin mesaj/sn. Bu hesap sonradan yapıldı (hipotez dışı), yalnızca açıklama olarak okunmalı; mekanizma ayrıca sınanmadı. Üstel kuralın dönüş dalgası üç kesinti süresinde de eşzamanlı olduğu için biriken yük tek seferde geliyor.
2. **Rastgeleli kuralda tepe kesinti süresiyle tekdüze değil:** 242 (15 sn) → 49 (60 sn) → 1.529 ms (180 sn). 180 sn'deki artış kapasiteye yaklaşmayla uyumlu (biriken ≈ 540 bin mesaj; sunucu CPU %96, bu koşularda sunucu doyuyor). 15 sn'deki yüksek değer ise açıklanamadı; olası ama sınanmamış bir neden: 15 sn'de bekleme penceresi henüz tavana (30 sn) ulaşmadığı için cihazlar daha dar bir aralığa yayılıyor.
3. **Üstel + B1, 180 sn'de sunucuyu doyuruyor:** sunucu CPU %100, eşitleme zaman aşımı 676-1.101 (60 sn'de 0), eşitleme p99 ≈ 7,6 sn. 60 sn'de ise doyma yalnızca kısa süreli.
4. **Model bu sınavda orta karar:** 6 hücrenin 4'ünde 2 kat içinde, ama üstel kuraldaki doyma öngörüsü yanlış: model tepenin 15 ve 60 sn'de aynı (9,3 sn) olacağını söylüyor, ölçüm 1,9 ve 9,1 sn. Yani model 60 sn dışındaki kesinti sürelerine **çıkarım için güvenilir değil**.
5. **Kayıp:** önceki Mosquitto koşularının en büyük kaybı 132 idi (N = 10.000 matrisi, sürüm v7; EMQX pilotundaki 272.526 ayrı bir hata düzeneğiydi). Bu matriste iki koşuda 255 ve 314; ikisi de aynı hücrede (rastgeleli, 15 sn) ve ikisinde de yüksek eşitleme zaman aşımı sayısı var. Sıfır kayıplı üstel 180 sn koşularında da 676-1.101 zaman aşımı olduğu için zaman aşımı sayısı tek başına nedeni açıklamıyor. Neden bilinmiyor.

### Sınırlar

- Yalnızca B1, Mosquitto, TLS'siz, N = 5.000, temiz kesinti. B2/B3, TLS, EMQX ve sessiz kesintide kesinti süresi taranmadı.
- Üç kesinti süresi (15, 60, 180); aradaki değerler ve daha uzun süreler (örn. 600 sn) denenmedi. 180 sn'de üstel kural 30 sn tavanındadır; tavan değişirse sonuç değişebilir.
- Hücre başına 4 tekrar; güven aralığı verilmiyor, aralık gösteriliyor. Kesinti süreleri aynı blokta rastgele sırada koşuldu; 60 sn hücresi bu blokta yeniden ölçüldü (adım 5'teki 9.158 ile uyumlu: 9.066).
- Kayıp nedeni için ham kayıt kullanılmadı.
- Adım 5'in tüm sınırları geçerli.
