# 17 — Adım 11: İş yükü duyarlılığı (vital ve alarm sıklığı)

Tarih: 19 Ekim 2026. Durum: **tasarım ve hipotezler yazıldı, koşular başlamadan önce** (sonuçlar bu dosyanın altına eklenecek).

## Neden bu deney

Şimdiye kadarki bütün koşularda her cihaz **2 sn'de bir vital** ve **10 sn'de bir alarm** mesajı üretti (128 bayt, QoS 1). Bu sıklıkların seçim gerekçesi deney günlüğüne ve karar defterine **yazılmamıştır** (19 Ekim'de fark edildi). Yalnızca "varsayımsal; klinik gerçekliği temsil etmez" notu vardır (günlük 06, 13). Gerçek bir izleme sisteminde alarm çok daha nadirdir.

Hesaba göre (60 sn kesinti, cihaz başına biriken): 30 vital + 6 alarm = 36 mesaj; 5.000 cihazda yaklaşık 180 bin mesaj, bunun **%83'ü vital**. Bu iki soruyu doğurur:

1. **Ana bulgumuz (30 ms → yaklaşık 9 sn, üstel + B1) iş yüküne ne kadar duyarlı?** Gecikmenin büyüklüğünü esas olarak biriken toplam mesaj sayısı (yani **vital hızı**) belirliyorsa, alarm sıklığı onu az etkilemeli; vital hızı düşünce gecikme belirgin düşmeli. (Gecikme ≈ biriken mesaj / sunucunun boşaltma hızı; Adım 10 ile uyumlu, bkz. günlük 16 gözlem 1.)
2. **B3'ün bedeli (üstel + TLS'te canlı alarm B2'ye göre 2,26 kat kötü; günlük 13) alarm sıklığının yüksek seçilmesinden mi kaynaklanıyor?** B3 birikmiş alarmları hemen gönderir: bu koşulda cihaz başına ≈ 6-7, toplam ≈ 32 bin mesaj aynı anda. Alarmlar nadir olsaydı bu yığın küçük olurdu.

## Tasarım

Sunucu Mosquitto 2.1.2 (2 CPU), N = 5.000, temiz kesinti 60 sn, üstel kural, 3 blok (tohum 1-3), blok içinde hücre sırası rastgele. İş yükü, koşu adına `-v<sn>` (vital aralığı) ve `-a<sn>` (alarm aralığı) eki olarak yazılır; varsayılan (2 / 10) ek almaz.

| Bölüm | Koşul | Hücreler | Koşu | Sürüm | İzleme |
|---|---|---|---|---|---|
| **A** | TLS'siz, B1 | iş yükü: (vital 2, alarm 10) temel; (2, 300) alarm nadir; (10, 10) vital 5 kat seyrek | 3 × 3 = 9 | `i1a` | 90 sn |
| **B** | TLS, B2 (30/200) ve B3 (30/200) | iş yükü: (2, 10) temel; (2, 300) alarm nadir | 2 × 2 × 3 = 12 | `i1b` | 180 sn |

Bölüm B, günlük 13'teki P2 matrisinin üstel + TLS hücrelerinin aynısıdır (B2 ve B3 aynı blokta). Temel iş yükü hücresi **bu blokta yeniden koşulur**; günlük 13'teki 1.524 / 673 ms ile karşılaştırma yalnızca yön göstergesidir (bloklar arası değişkenlik büyük).

Biriken mesaj sayısı, iş yüküne göre cihaz başına (60 sn kesinti): (2, 10) → 30 + 6 = 36; (2, 300) → 30 + 0,2 = 30,2; (10, 10) → 6 + 6 = 12.

## Hipotezler (sonuçları görmeden yazıldı)

Ölçüt: 3 bloğun medyanı. "Tepe" = kesinti sonrası canlı alarm p99'un en büyük saniyelik değeri.

- **HW1 (Bölüm A).** Alarm sıklığı 10 sn'den 300 sn'ye inince B1 tepesi **temel değerin ±%25'i içinde** kalır (biriken mesaj yalnızca ≈ %16 azalır; yük esas olarak vital).
- **HW2 (Bölüm A).** Vital sıklığı 2 sn'den 10 sn'ye inince (biriken mesaj ≈ %67 azalır) B1 tepesi **temelin en az %50 altına** iner.
- **HW3 (Bölüm B, önkoşul).** Temel iş yükünde B3 tepesi / B2 tepesi ≥ 1,5 çıkar (günlük 13'teki bedel bu blokta yeniden görülür). **Bu sağlanmazsa HW4 anlamsızdır** ve "bedel bu blokta yeniden üretilemedi" diye raporlanır.
- **HW4 (Bölüm B).** Alarm 300 sn'de B3 tepesi / B2 tepesi ≤ 1,5 olur (bedel iş yükü varsayımına bağlıdır; alarm nadirken kaybolur).
- **HW5 (Bölüm B).** B3'ün birikmiş alarm yaşı kazancı (B2'ye göre yaş medyanı düşüşü) her iki alarm sıklığında ≥ %25 kalır. *Çürüyebilir:* alarm nadirken cihazların çoğunun birikmiş alarmı yoktur ve yaş istatistiği az örnekle hesaplanır.
- **HW6.** Tekrar 0 ve kayıp ≤ 1 (tüm 21 koşuda).

## Bilinen ölçüm sorunu (önceden)

Alarm sıklığı 300 sn olduğunda saniyede yaklaşık 5.000 / 300 ≈ 17 canlı alarm örneği vardır; **saniyelik p99 17 örnekten hesaplanır ve gürültülüdür** (temelde saniyede ≈ 500 örnek). Bu yüzden alarm p99 tepesinin yanında aynı kuyruğu paylaşan **canlı vital** p99 tepesi (`vital_tepe_p99_ms`, analizde var) kontrol ölçütü olarak raporlanır; HW1, HW2 ve HW4 iki ölçütle de okunur, ayrışırlarsa belirtilir.

## Koşu komutları

```
./scripts/matris5.ps1 -Tekrar 3 -Cihazlar 5000 -Politikalar ustel -BPolitikalari b1 -IsYukleri "2:10","2:300","10:10" -SonrasiSn 90 -Surum i1a -Not "Adim 11 A"
./scripts/matris5.ps1 -Tekrar 3 -Cihazlar 5000 -Politikalar ustel -BPolitikalari b2,b3 -IsYukleri "2:10","2:300" -Tls -SonrasiSn 180 -Surum i1b -Not "Adim 11 B"
```

Tahmini süre: Bölüm A ≈ 40 dk, Bölüm B ≈ 80 dk.

## Sonuçlar

(Koşular bitince eklenecek.)
