# 04 — Adım 3: Büyük cihaz sayısı, sunucu 2 CPU / 2 GB, simülatör Docker içinde

Tarih: 13 Ekim 2026
Durum: **Devam ediyor** (ara kademe koşuları sürüyor; bu bölüm kesinleşmiş bulguları içerir)
Rapor bölümü: Yöntem (ölçüm doğrulama), Bulgular (ön), Sınırlar

## Amaç ve beklenti (sonuçlar görülmeden önce yazıldı)

Cihaz sayısı arttığında temiz kesinti sonrası toplu yeniden bağlanma sunucuyu zorlar mı ("fırtına")? Zorlanma işaretleri: sunucu AÇIKKEN başarısız bağlantı, sunucu CPU'sunun sınıra yaklaşması, toparlanma süresinin cihaz sayısıyla hızla artması.

Beklenti: Mosquitto TLS'siz olduğu için 5.000 eşzamanlı bağlantı kolay taşınır, fırtına çıkmayabilir (plan `14`, Bölüm 7, Risk 1).

## Kurulum (v2)

| Öğe | Değer |
|---|---|
| Simülatör | Docker konteyneri (`mqtt-sim`), 4 CPU, 3 GB, sunucuya `broker:1883` ile Docker ağı içinden |
| Sunucu | Mosquitto 2.1.2, **2 CPU, 2 GB** (konteyner sınırı), TLS yok, kimlik doğrulama yok |
| Docker/WSL | Varsayılan: 12 CPU, ~8 GB (`.wslconfig` yok) |
| Cihaz | N = 500…5.000, kural `sabit` (1 sn), tohum 1 |
| İlk bağlanma | [0, 20 sn]'ye rastgele yayılır (başlangıç fırtınası olmasın) |
| Zaman çizelgesi | t=40 sn kesinti (temiz, `docker stop`), ~60 sn kapalı, sonra ~120 sn toparlanma |
| Betik | `scripts/adim3.ps1`, özet: `scripts/ozet.ps1 -Onek adim3` |

## Bulgu 1 — v1 koşuları GEÇERSİZ (ölçüm hatası)

İlk dört koşu (N = 500, 1.000, 2.000, 5.000; dosya adı `_v1`) "fırtına çıkmadı" gibi görünüyordu (sunucu CPU'su %1–39, toparlanma 4–5 sn). Saniye saniye deneme sayısına bakınca kesinti sırasında **neredeyse hiç yeniden deneme olmadığı** görüldü: 5.000 cihaz t=42'de bir kez denedi, sonra 60 sn boyunca toplam ~15 deneme.

**Neden:** Yanıt vermeyen (durdurulmuş) sunucuya bağlanırken SYN'e hiç cevap gelmiyor. MQTTnet'in `WithTimeout` ayarı bu TCP bağlantı kurulumunu sınırlamıyordu. Cihazlar tek bir asılı denemede bekledi, **bekleme kuralı hiç çalışmadı.** Sunucu açılınca asılı denemeler tamamlandı.

**Düzeltme:** `ConnectAsync` çağrısına `CancelAfter(5 sn)` ile kendi zaman aşımımızı koyduk (`Device.cs`). Hata türü `BaglantiZamanAsimi` olarak sınıflanıyor.

**Ders (raporda yazılabilir):** Toplam "bağlandı" sayısı doğru görünse bile, kesinti sırasındaki **deneme sayısı** ayrıca doğrulanmalı. v1 koşuları silinmedi; kayıt defterinde "GEÇERSİZ" notuyla duruyor.

## Bulgu 2 — v2, 5.000 cihaz: sunucu açıldıktan sonra uzun gecikme (ön, tek koşu)

Koşu: `adim3_sabit_n5000_s1_v2`, kod `c069b94`.

| Ölçüt | Değer |
|---|---|
| Kesintide (60 sn) deneme dalgaları | Her ~6 sn'de bir **~5.000'lik dalga** (5 sn zaman aşımı + 1 sn bekleme), tam kilit adım |
| Sunucu açılış anı | t ≈ 103 sn |
| Sunucu açıldıktan sonraki başarısız bağlantı | **55.001** (aynı dalga yapısı ~65 sn daha sürdü) |
| Tüm cihazların bağlanması (TTR %100) | **~69 sn** (t ≈ 172) |
| Sunucu CPU (örneklenen en yüksek) | %33 (sınır 200 = 2 çekirdek) |
| Sunucu belleği | Açıldıktan sonra ~65 sn boyunca **2,3 MiB sabit**, yani bağlantı kabul etmemiş. t≈170'te hepsi birden bağlandı |

Yorum (ön): Sunucu yükü yüzünden değil. Sunucu o sürede boştu ama cihazların bağlantı denemeleri zaman aşımına uğradı.

## Bulgu 3 — Tanı deneyleri

1. **Yoklayıcı konteyner** (alpine, her sn `nc -z broker 1883`): sunucu durdurulup açıldığında bağlantı **açılış saniyesinde** yeniden kuruldu. Sunucunun IP'si aynı kaldı (172.21.0.2), **MAC adresi değişti** (`e2:f2:67:0c:26:7f` → `b6:bd:dd:9c:75:b2`). Durdurma sırasında alpine'de `nc: bad address 'broker'` hatası (Docker adı çözmüyor).
2. **50 cihazlı aynı koşu** (`adim3_sabit_n50_s1_tani`, 30 sn kesinti): sunucu açıldıktan ~9 sn içinde 50/50 bağlı (son başarısız dalga t=50–51, t=60'ta 50 bağlı).

Sonuç: Ağ yolu hızla düzeliyor ve küçük N'de toparlanma hızlı. 5.000'de uzun gecikme **cihaz sayısına bağlı**. Nedeni **bilinmiyor.**

Olası açıklamalar (**hiçbiri sınanmadı**): Docker köprü ağı / bağlantı izleme (conntrack) davranışı çok sayıda eşzamanlı SYN altında; Mosquitto'nun dinleme kuyruğu; simülatör tarafı. Ara kademe koşuları gecikmenin hangi N'de başladığını gösterecek. Kesin neden bulunmadan "sunucu devrildi" ya da "ortam artefaktı" denmemeli.

## Sınırlar (raporda yazılmalı)

- v2 sonuçları **tek koşu, tek tohum**.
- Sunucu CPU örnekleme sıklığı yaklaşık 0,7 Hz (docker stats çağrısı ~1,5 sn sürüyor). Çok kısa CPU tepeleri kaçırılmış olabilir. "CPU düşük" ifadesi bu sınırla birlikte yazılmalı.
- `docker stats` CPU yüzdesi çekirdek başına (100 = 1 çekirdek).
- Çözünürlük 1 sn.
- Tek laptop, ağ sıfır gecikmeli, TLS yok.

## Sonraki adım

Ara kademe koşuları (`_v2b`, N = 500, 1.000, 2.000, 5.000; simülatör CPU'su da örnekleniyor) bitince: gecikmenin başladığı N'yi belirle, 5.000'in tekrarlanabilirliğine bak, nedeni daraltmak için bir tanı daha planla.

## Dosyalar

`results/adim3_*_v1.*` (geçersiz), `adim3_sabit_n5000_s1_v2.*`, `adim3_sabit_n50_s1_tani.*`, `adim3_*_v2b.*`, `results/ozet_adim3.txt`, `scripts/adim3.ps1`, `scripts/ozet.ps1`

## Bulgu 4 — Ara kademe koşuları (`_v2b`, tek koşu/hücre) ve 5.000'in tekrarı

Aynı kod (`da524d1`), aynı ayarlar, sabit kural, tohum 1. Sunucu açıldıktan sonra toparlanma:

| N | TTR %50 / %95 / %100 (sn) | Sunucu açıkken başarısız bağlantı (≈ 6 sn'lik dalga sayısı) | Sunucu CPU tepe (%) | Simülatör CPU tepe (%, sınır 400) |
|---|---|---|---|---|
| 50 (tanı) | 2,1 / 2,1 / 2,1 | 0 | 1 | - |
| 500 | 3,5 / 4,5 / 4,5 | 500 (1 dalga) | 7 | 65 |
| 1.000 | 8,2 / 9,2 / 9,2 | 1.000 (1 dalga) | 5 | 143 |
| 2.000 | 11,5 / 12,5 / 12,5 | 4.000 (2 dalga) | 12 | 161 |
| 5.000 (v2b) | 26,1 / 27,1 / 27,1 | 20.000 (4 dalga) | 30 | 177 |
| 5.000 (v2, ilk koşu) | 68,3 / 69,3 / 69,3 | 55.001 (11 dalga) | 33 | - |

Gözlemler:
1. **Aynı N=5.000, aynı ayarlar, iki koşu: 27 sn ve 69 sn.** Koşular arası değişkenlik çok büyük; tek koşuyla N etkisi ile rastlantı ayrılamaz.
2. **Toparlanma "ya hep ya hiç" biçiminde.** Sunucu açıldıktan sonra cihazlar birkaç dalga boyunca (her ~6 sn) tamamen başarısız oluyor, sonra ~1-2 sn içinde tüm cihazlar birden bağlanıyor (örn. 5.000 v2b: t126'da 0 bağlı, t127'de 1.036, t128'de 3.723). Kademeli bir sunucu doygunluğu deseni değil.
3. **Sunucu ve simülatör CPU'su sınırlara yakın değil** (sunucu en çok %30 / 200; simülatör en çok %177 / 400; örnekleme sınırı için bkz. Sınırlar).

**Yorum (ön, sınanmadı):** Tüm sanal cihazlar tek bir konteynerde, yani **tek bir ağ yığınını (network namespace) paylaşıyor.** Sunucu konteyneri yeniden başlatılınca MAC adresi değişiyor (bulgu 3). Cihazların hepsinin ortak kullandığı çekirdek durumu (örn. komşu/ARP tablosu girdisi) tek bir olayda geçerli hale geliyor olabilir. Bu, "birkaç dalga hiçbir şey, sonra hepsi birden" desenini ve koşular arası büyük değişkenliği açıklayabilir. **Bu bir hipotez.** Sınama: sunucu konteynerine sabit MAC (`mac_address`) vermek. Gecikme kaybolursa, gözlenen gecikme MQTT fırtınası değil, konteyner yeniden başlatmanın bir yan etkisidir (gerçek bir sunucu yeniden başladığında ağ kartının MAC adresi değişmez).

## Sınırlar (Bulgu 4 için)

- Her hücre **tek koşu**. Değişkenlik büyük olduğundan N'ye bağlı hiçbir eğilim iddia edilemez.
- Sunucu/simülatör CPU örnekleme ~0,7 Hz.
- Dalga sayısı `hata_sonra / N` ile yaklaşık hesaplandı (ilk dalganın bir kısmı sunucu açılışından önce başlamış olabilir).
