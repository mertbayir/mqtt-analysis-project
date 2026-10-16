# 06 — Adım 5: Bağlantı sonrası yük ve canlı alarm gecikmesi

Tarih: 15 Ekim 2026
Durum: **Matris tamamlandı (18/18 koşu, sürüm v6)**. Hipotezler matris sonuçları görülmeden önce yazıldı; pilot koşuların sonucu yazım sırasında biliniyordu (bkz. "Ön bilgi")
Rapor bölümü: Yöntem (3.4 ölçütler, 3.6 ölçüm doğrulama), Bulgular (5.4-5.6)

## Amaç

Projenin ana iddiası: bağlanma aşaması dağıtılsa bile, cihazların bağlandıktan sonraki işi (durum bilgisi eşitleme + kesintide biriken mesajları boşaltma) **canlı alarm mesajlarını geciktirir mi** ve bu işi bağlanmadan ayrı zamanlamak (B2) bunu düzeltir mi, bedeli ne olur?

## Kurulum

| Öğe | Değer |
|---|---|
| Cihaz iş yükü | Vital bulgu her **2 sn**, alarm her **10 sn** (cihaz başına rastgele faz), mesaj 128 bayt, QoS 1; mesajın **planlanan** gönderim anı önceden belirli (açık döngü) |
| Biriken | Bağlı değilken (kesinti) üretilen mesajlar bellek kuyruğunda; bağlanınca B politikasına göre işlenir |
| Eşitleme | MQTT üzerinden istek/cevap: cihaz `sync/ist/<id>` yayınlar, toplayıcı `sync/yanit/<id>` ile 4.096 bayt döner; cihaz 10 sn zaman aşımı |
| Toplayıcı | Ayrı konteyner (2 CPU, 1 GB), tüm cihaz mesajlarını dinler, **gecikme = alınma anı − planlanan an** (tek yönlü saat), sunucu kapanıp açılınca 100 ms'de geri bağlanır |
| Ölçüm saati | **CLOCK_MONOTONIC** (aynı çekirdeği paylaşan konteynerler arası ortak); duvar saati WSL2'de sıçradığı için kullanılmadı (Bulgu 1) |
| Sunucu | Mosquitto 2.1.2, 2 CPU / 2 GB, TLS yok, sabit MAC, IP ile erişim; **toplayıcı için kuyruk sınırsız** (`max_queued_messages 0`, `max_inflight_messages 100`) |
| Kesinti | Temiz, 60 sn; ilk bağlanma 20 sn'ye yayılı; kesinti t=40 sn; toparlanma 90 sn izlenir; üretim koşu bitmeden 5 sn önce durur |
| B politikaları | **B0** referans (biriken atılır, eşitleme yok), **B1** hemen (eşitle, tüm birikeni peş peşe boşalt), **B2** ayrı zamanlı (bağlandıktan sonra U(0,30) sn bekle, eşitle, birikeni 200 ms aralıkla boşalt) |
| A politikaları | **üstel** (dönüş kilit adım, en kötü senaryo), **rastgeleli üstel** (dönüş dağınık) |
| İlk bağlanma | Eşitleme ve boşaltma **yalnızca yeniden bağlanmada** (ilk açılışta yok) |

## Bulgu 1 — Ölçüm doğrulama: WSL2 duvar saati sıçraması (ön koşular sırasında bulundu)

İlk pilot koşuda canlı alarm gecikmesi bazı saniyelerde **negatif** çıktı (−2,3 sn, −4,7 sn): bir mesajın planlanan andan önce alınması fiziksel olarak imkânsız, yani ölçüm hatası. Adli inceleme:

1. Toplayıcıya ham kayıt seçeneği eklendi (`RAW_PATH`). Hata, her cihazda **tek bir mesajın** ~2,3 sn **erken** gönderilmesiydi: cihaz zamanlayıcısı (`Task.Delay`) mesaj zamanından önce uyanıyor, kodum uyandıktan sonra kontrol etmiyordu. Kontrol eklendi (mesaj asla planlanan andan önce gönderilmez).
2. **Saat bekçisi** eklendi (duvar saati ile tek yönlü sayaç farkı 100 ms'de bir izlenir). Bekçi duvar saatinde ani sıçramalar yakaladı: örn. **+3.011 ms ve 100 ms sonra −3.487 ms**; 60 sn'lik bir koşuda iki çift sıçrama (≈ her 30 sn'de bir). Her koşuda `olay.csv` içinde `saat_sicramasi` olarak kayıtlı.
3. **Kalıcı çözüm:** planlanan an ve alınma anı **tek yönlü saatle** (CLOCK_MONOTONIC) ölçülüyor. Doğrulama: 1.560 mesajlık 60 sn'lik koşuda, iki saat sıçraması olmasına rağmen gecikme 0-20 ms, negatif yok.

**Ders (raporda yazılabilir):** WSL2/Docker'da duvar saatiyle yapılan gecikme ölçümleri, ~30 sn'de bir ±3 sn'lik sıçramalarla bozulabilir. Konteynerler arası gecikme ölçümü için tek yönlü saat kullanılmalı.

## Ön bilgi (matris öncesi pilotlar, nihai veri değil)

| Pilot | Koşu | Sonuç |
|---|---|---|
| `pilot` (N=100) | boru hattı doğrulaması | 5.639 mesaj üretildi = canlı 2.705 + biriken 2.934; hepsi toplayıcıya ulaştı; 0 kayıp |
| `pilot2` (N=500, üstel/B1) | | canlı alarm p99: taban 19 ms → tepe 916 ms; boşaltma tepe ~20.000 mesaj/sn |
| `pilot3` (N=5.000, üstel/B1) | | canlı alarm p99: taban 37 ms → tepe **10.299 ms**, 23 sn boyunca >100 ms; boşaltma tepe 23.242 mesaj/sn; sync p50/p99 3,0/8,4 sn; sunucu CPU tepe %91; 539.656 mesaj, 0 kayıp, 0 tekrar |

Pilotlar H5'in yönünü bildirdiği için H5 "çürüyebilir bir hipotez" olarak değil, **tekrarlı koşuyla nicel doğrulama** olarak okunmalı.

## Hipotezler (matris öncesi yazıldı)

| # | Hipotez | Not |
|---|---|---|
| H5 | **Üstel A + B1:** sunucu geri açıldıktan sonra canlı alarm gecikmesinin saniyelik p99 tepesi, kesinti öncesi tabanın en az 10 katına çıkar ("ikinci izdiham") | Pilotta gözlendi (37 ms → 10 sn); tekrarlı koşuyla doğrulanacak |
| H6 | **B2, B1'e göre** (aynı A ile) canlı alarm p99 tepesini en az %50 düşürür; bedeli boşaltmanın bitiş süresinin uzamasıdır | **Çürüyebilir** |
| H7 | **Rastgeleli A + B1**, **üstel A + B1**'e göre canlı alarm p99 tepesini düşürür (dönüşün dağıtılması bağlantı sonrası yükü de dağıtır) | **Çürüyebilir** |
| H8 | **B0 (referans)** canlı alarm p99'unu taban düzeyine yakın tutar (bağlanma aşaması tek başına ikinci izdiham yaratmaz); p99 100 ms eşiğini 5 sn'den fazla aşmaz | **Çürüyebilir** |
| H9 | Tüm kombinasyonlarda kayıp 0 ve tekrar 0 (QoS 1 + biriktirme) | **Çürüyebilir** |
| H10 | B1'de sunucu CPU tepesi tek çekirdek sınırına yakındır (≥ %80), çünkü Mosquitto tek iş parçacıklıdır; darboğaz sunucunun tek çekirdeğidir | Keşif amaçlı |

Değişirse bu tablo silinmez, karar defterine tarihle yazılır.

## Tasarım

| Öğe | Değer |
|---|---|
| Faktörler | A (2: üstel, rastgeleli üstel) × B (3: b0, b1, b2) = 6 hücre |
| Cihaz sayısı | 5.000 (ilk matris). 2.000 sonra eklenebilir |
| Tekrar | 3 blok (tohum 1, 2, 3), her blokta 6 hücre rastgele sırayla |
| Toplam | 18 koşu, ~80 dk |
| Sürüm etiketi | `v6` |
| Betikler | `scripts/adim5.ps1` (koşu), `scripts/matris5.ps1` (matris), `scripts/analiz_adim5.py` (analiz) |

## Analiz planı (önceden)

- Hücre başına 3 koşu: **medyan ve aralık**; güven aralığı iddiası yok.
- Birincil ölçüt: canlı alarm saniyelik p99'un sunucu açıldıktan sonraki **tepesi** ve **100 ms'yi aştığı saniye sayısı**; taban kesinti öncesi saniyelik p99'ların medyanı.
- İkincil: boşaltmanın bitiş süresi (basla'dan son biriken mesaja), eşitleme süresi p50/p99, sunucu CPU tepesi, kayıp/tekrar.
- Aykırı koşu çıkarılmaz. Teknik başarısız koşu (betik hatası, toplayıcı bağlanamadı) nedenle işaretlenir.

## Sonuçlar

### Koşu akışı (kayıt)

İlk deneme 4. koşuda Docker Desktop'ın kapanması yüzünden yarım kaldı; bunu sürdürme girişimi de betik hatası yüzünden ilk 3 koşunun simülatör dosyalarını sildi. Bu 4 koşu **geçersiz** sayıldı (dosyalar `results/iptal_docker_kapandi/` ve `results/iptal_sim_dosyalari_silindi/` altında, kayıt defteri satırları çıkarıldı, matris günlüklerinde nedeni yazılı). Matris baştan koşuldu: `results/matris5_20261016_1118.txt`, 18 koşu, 0 betik hatası.

### Hücre başına sonuçlar (N = 5.000, 3 tekrar, medyan [en küçük - en büyük])

Kesinti öncesi taban (canlı alarm saniyelik p99): **26-37 ms** (tüm koşularda).

| A | B | Alarm p99 tepe (ms) | >100 ms süre (sn) | Boşaltma bitişi (sn) | Eşitleme p99 (ms) | Sunucu CPU tepe (%) | Atılan mesaj |
|---|---|---|---|---|---|---|---|
| üstel | B0 | 47 [44-51] | 0 | - | - | 37 [33-38] | 259 bin (%48) |
| üstel | B1 | **9.158** [8.821-10.413] | 19 [18-23] | 42,6 [41,6-45,0] | 8.147 [7.936-9.975] | **98** [98-98] | 0 |
| üstel | B2 | 143 [135-145] | 17 [16-19] | 65,6 [65,2-65,8] | 126 [125-137] | 58 [57-64] | 0 |
| rastgeleli | B0 | 60 [47-70] | 0 | - | - | 21 [19-24] | 219 bin (%41) |
| rastgeleli | B1 | 51 [49-66] | 0 | 28,9 [28,2-29,5] | 6 [5-7] | 85 [80-87] | 0 |
| rastgeleli | B2 | 79 [72-112] | 0 [0-2] | 63,7 [62,2-65,6] | 56 [44-60] | 59 [58-61] | 0 |

Boşaltma tepe hızı (saniyede toplayıcıya ulaşan biriken mesaj): üstel B1 ≈ 22-26 bin, üstel B2 ≈ 9,6-9,9 bin, rastgeleli B1 ≈ 15 bin, rastgeleli B2 ≈ 7,2 bin.

### Hipotezlerin durumu

| # | Sonuç | Kanıt |
|---|---|---|
| H5 | **Destekleniyor** | Üstel + B1'de tepe taban değerin ≈ 290-350 katı (8.821-10.413 ms, taban ≈ 30 ms). Aynı A ile B0'da tepe 47 ms; yani etki bağlanmanın kendisinden değil, bağlantı sonrası işten geliyor |
| H6 | **Kısmen çürüdü** | Üstel: B2, B1'e göre tepeyi **%98,4** düşürdü (9.158 → 143 ms), bedeli boşaltma bitişi +23 sn (42,6 → 65,6). Rastgeleli: B1 zaten tabana yakın (51 ms), B2 **daha yüksek** çıktı (79 ms, %55 artış, küçük mutlak değer) ve boşaltma 2,2 kat uzadı. "Aynı A ile en az %50 düşürür" yalnızca üstelde doğru |
| H7 | **Destekleniyor** | B1 ile rastgeleli A, üstel A'ya göre tepeyi %99,4 düşürdü (9.158 → 51 ms) ve boşaltmayı 14 sn daha erken bitirdi |
| H8 | **Destekleniyor** | B0'da tepe 47 ve 60 ms, 100 ms eşiği hiç aşılmadı. Not: B0 bunu birikeni atarak sağlıyor (üretilen mesajın %48'i ve %41'i çöpe gidiyor), yani referans olarak anlamlı ama kullanılabilir bir politika değil |
| H9 | **Kısmen çürüdü** | 18 koşunun 16'sında kayıp 0; **rastgeleli/B1'in 2 koşusunda (tohum 1 ve 3) 539.62x mesajdan 1'i toplayıcıya ulaşmadı**. Tekrar (çift alınan) her yerde 0 |
| H10 | **Destekleniyor** | B1'de sunucu CPU tepesi üstelde %98, rastgelelide %85 (en küçük koşu %79,6). Mosquitto tek çekirdeği doyuyor. B2'de %58-59, B0'da %21-37 |

### Gözlemler

1. **İkinci izdiham gerçek ve yalnızca kilit adım dönüşte oluşuyor.** Üstel kuralda 5.000 cihaz birkaç saniye içinde geri dönüyor, hepsi aynı anda eşitleme istiyor ve birikeni boşaltıyor. Canlı alarm p99'u ≈ 30 ms'den ≈ 9 sn'ye çıkıyor, p99 ≈ 19 sn boyunca 100 ms'nin üstünde kalıyor, sunucu tek çekirdeği doyuyor ve eşitleme isteklerinin p99'u 8 sn'ye (10 sn zaman aşımına çok yakın) çıkıyor. Tohum 1'de 50 cihazın eşitlemesi zaman aşımına uğradı (diğer iki tekrarda 0).
2. **Dönüşü dağıtmak bağlantı sonrası yükü de dağıtıyor.** Rastgeleli üstel kuralda B1 bile tepe ≈ 51 ms ile kalıyor; yani Adım 4'te bulunan "rastgeleli kural" avantajı burada da işe yarıyor ve ek bir B politikasına gerek bırakmıyor (en azından bu ölçekte).
3. **B2 iş görüyor ama yalnız kilit adım dönüşte.** Üstel kuralda B2, p99'u 9 sn'den 143 ms'ye indiriyor. Bedeli: boşaltma 23 sn daha geç bitiyor ve eşitleme 30 sn'lik pencereye yayılıyor. Rastgeleli kuralda B2 gereksiz, hatta küçük bir kötüleşme yaratıyor. Üstel/B2'deki "17 sn eşik üstü" sayısı yanıltıcı: tepe 143 ms, eşiğin yalnızca 1,4 katı.
4. **En iyi denge (bu veriyle): rastgeleli A + B1.** Tepe ≈ 51 ms, boşaltma en kısa sürede (≈ 29 sn) bitiyor, kayıp yok. Bedeli sunucu CPU'sunun %85'e çıkması; yani bu hücre ölçek büyüdükçe (N > 5.000) doyma noktasına en yakın duran hücre. **Bu ek kapasite deneyi gerektirir** (aşağıda).
5. **Eşitleme süresi üstel B1'de bağlantı kurulumu kadar ağır bir sorun.** Bu, yalnızca alarm gecikmesi değil, cihazın "bağlandı ama henüz hazır değil" süresidir.

### Sınırlar ve açık noktalar

- **H9'daki 2 kayıp açıklanmadı.** Elde olan tek ipucu: mesajlar cihaz tarafında üretilmiş (üretilen = canlı + biriken eşit), toplayıcıya 1 mesaj ulaşmamış. Olası neden (doğrulanmadı): sunucu durdurulduğu anda sunucunun onayladığı ama toplayıcıya henüz iletmediği mesaj, bellek-içi olduğu için sunucuyla birlikte kayboldu (QoS 1 yalnızca teslim onayını cihaza verir; Mosquitto'da kalıcılık kapalı). Kesin kanıt için toplayıcıya ham kayıt (`RAW_PATH`) açılıp tekrar koşmak gerekir.
- Her hücrede yalnızca **3 tekrar**; aralıklar dardır ama güven aralığı iddiası yok. Rastgeleli B2'nin tepesindeki [72-112] aralığı geniş (tek koşu 2 sn eşik aştı).
- Tek sunucu, tek çekirdekli Mosquitto, TLS yok, temiz tek kesinti; sunucu CPU örnekleme ~0,7 Hz. Rastgeleli B1'in CPU'su %85: N büyüdüğünde sonucun aynı kalacağı **varsayılamaz**.
- Toplayıcı tek istemci; kendi yavaşlaması gecikmeye karışmış olabilir (ölçülmedi).
- B0'ın "atılan" mesajları: gerçek bir sistemde kabul edilemez, yalnızca referans.

## Sınırlar (önceden bilinen)

- TLS'siz, tek sunuculu, tek makine, sanal cihaz, temiz kesinti tek tür.
- Mosquitto tek iş parçacıklı; bulgular çok çekirdekli bir sunucuya genellenemez (gelecek çalışma: EMQX).
- Toplayıcı tek istemci (QoS 1, sınırsız kuyruk); onun yavaşlaması gecikmeye karışabilir. Toplayıcının işlem yükü ölçülmedi.
- Sunucu CPU örnekleme ~0,7 Hz.
- Docker'da başka bir projeye ait boşta konteynerler çalışıyor olabilir.
- Mesaj boyutu, vital/alarm sıklığı ve eşitleme boyutu varsayımsal; klinik gerçekliği temsil etmez.

## Dosyalar

`results/adim5_*_v6.*`, `results/ozet_adim5_v6_*.csv`, `scripts/adim5.ps1`, `scripts/matris5.ps1`, `scripts/analiz_adim5.py`, `src/Collector/`, `src/Simulator/{Device,PostConnectPolicy,Stats}.cs`
