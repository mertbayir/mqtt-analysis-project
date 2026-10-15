# 06 — Adım 5: Bağlantı sonrası yük ve canlı alarm gecikmesi

Tarih: 15 Ekim 2026
Durum: **Matris başlıyor** (aşağıdaki hipotezler, matris sonuçları görülmeden önce yazıldı; pilot koşuların sonucu yazım sırasında biliniyordu, bkz. "Ön bilgi")
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

`[ ]` Matris bitince doldurulacak.

## Sınırlar (önceden bilinen)

- TLS'siz, tek sunuculu, tek makine, sanal cihaz, temiz kesinti tek tür.
- Mosquitto tek iş parçacıklı; bulgular çok çekirdekli bir sunucuya genellenemez (gelecek çalışma: EMQX).
- Toplayıcı tek istemci (QoS 1, sınırsız kuyruk); onun yavaşlaması gecikmeye karışabilir. Toplayıcının işlem yükü ölçülmedi.
- Sunucu CPU örnekleme ~0,7 Hz.
- Docker'da başka bir projeye ait boşta konteynerler çalışıyor olabilir.
- Mesaj boyutu, vital/alarm sıklığı ve eşitleme boyutu varsayımsal; klinik gerçekliği temsil etmez.

## Dosyalar

`results/adim5_*_v6.*`, `results/ozet_adim5_v6_*.csv`, `scripts/adim5.ps1`, `scripts/matris5.ps1`, `scripts/analiz_adim5.py`, `src/Collector/`, `src/Simulator/{Device,PostConnectPolicy,Stats}.cs`
