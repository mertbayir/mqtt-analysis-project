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

(Koşular bitince eklenecek.)
