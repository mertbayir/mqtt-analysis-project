# Hasta İzleme Gibi Çok Cihazlı MQTT 5 Sistemlerinde Toplu Yeniden Bağlanma ve Bağlantı Sonrası Yük: Bir Ölçüm Çalışması

*(Çalışma başlığı. Başlık, sonuçlar belli olunca netleştirilecek.)*

**Yazar:** Mert Bayır  
**Ders:** Bilimsel Araştırma, Proje Hazırlama ve Seminer, Sakarya Üniversitesi  
**Durum:** TASLAK, deney sürüyor. `[ ]` işaretli yerler doldurulacak.

---

## Özet

`[ ]` Sonuçlar belli olunca yazılacak. Yapı: problem (1 cümle), yöntem (2 cümle), bulgular (2-3 cümle, sayılar `ozet.ps1` çıktısından), sınır ve sonuç (1 cümle). Yaklaşık 150-200 sözcük.

## Abstract

`[ ]` Özetin İngilizcesi.

**Anahtar kelimeler:** MQTT 5, toplu yeniden bağlanma, üstel geri çekilme, bağlantı sonrası yük, nesnelerin interneti, hasta izleme

---

## 1. Giriş

*Durum: ilk taslak yazıldı, dil ve kaynaklar sonra cilalanacak.*

Çok sayıda cihazın tek bir merkezi sunucuya kalıcı bağlantıyla bağlandığı sistemlerde, sunucunun ya da ağın kısa süreli kesintisi cihazların hemen hemen aynı anda yeniden bağlanmaya çalışmasına yol açar. Hasta başı monitörleri ve giyilebilir sensörler gibi tıbbi nesnelerin interneti (IoMT) dağıtımlarında bu durum önem taşır: kesinti sırasında biriken ölçümler ve sunucudan alınması gereken güncel yapılandırma, cihazlar geri döndüğü anda sunucuya yeni bir yük bindirir ve bu yük, toparlanma sürerken akmaya devam eden canlı mesajları, örneğin alarmları, geciktirebilir.

Yeniden bağlanma anındaki bağlantı yükünü azaltmak için bilinen yöntem, cihazların bağlanmadan önce rastgele bir süre beklemesi ve başarısız denemelerde bekleme süresini üstel olarak artırmasıdır [1], [2]. Ancak cihaz bağlandıktan sonra da yük sürer: cihaz kesinti sırasında kaçırdığı durumu eşitler ve biriktirdiği mesajları gönderir. Bu *bağlantı sonrası yükün* bağlanma aşamasından ayrı ölçüldüğü, MQTT 5 üzerinde canlı trafik sürerken yapılmış açık bir çalışma, bildiğimiz kadarıyla bulunamamıştır. `[ ]` (arama kapsamı ve tarihi İlgili Çalışmalar'da yazılacak; Spillway okunduktan sonra bu cümle netleştirilecek)

Bu çalışma yeni bir algoritma önermez. Bilinen bekleme kurallarını, açık kaynaklı bir MQTT 5 sunucusu ve kendi geliştirdiğimiz bir cihaz simülatöründen oluşan tekrarlanabilir bir ortamda karşılaştırır. Katkılarımız: (i) tekrarlanabilir bir ölçüm ortamı ve kayıt düzeni, (ii) üç bekleme kuralının toplu yeniden bağlanma altında karşılaştırması, (iii) ölçüm ortamının kendisinin sonuçları nasıl çarpıtabildiğine dair doğrulanmış bulgular (Bölüm 4). (iv) bağlantı sonrası yükün (durum eşitleme ve biriken mesajların boşaltılması) canlı alarm gecikmesine etkisinin, bağlanma aşamasından ayrı olarak ölçülmesi ve üç boşaltma politikasının karşılaştırılması (Bölüm 5.5-5.7).

Çalışma tek bir bilgisayarda, sanal cihazlarla ve TLS'siz yapılmıştır; sonuçlar bu ortama göreceli olarak raporlanır, mutlak kapasite iddiası taşımaz. Tıbbi çerçeve bir senaryo olarak kullanılmıştır, klinik güvenlik iddiası yoktur.

## 2. İlgili Çalışmalar

*Durum: yalnızca [1] ve [2] doğrulandı. Diğerleri okunmadan yazılmayacak.*

- **Standartlar ve bilinen yöntemler:** RFC 6455 §7.2.3 ilk yeniden bağlanmanın rastgele geciktirilmesini ve sonraki denemelerde kesilmiş ikili üstel geri çekilmeyi önerir [1]. RFC 5626 §4.5 üst sınırı `W = min(en çok, taban × 2^hata)` olarak tanımlar ve gerçek beklemeyi W'nin %50-%100'ü arasında rastgele seçer [2]. Bu çalışmanın bekleme kuralları bu iki standarttan türetilmiştir (Bölüm 3.2).
- `[ ]` Metastabil çöküş ve yeniden deneme güçlenmesi (Huang ve ark., Isaacs ve ark.)
- `[ ]` Telekom çekirdek ağında senkron yeniden bağlanma (**Spillway**, en yakın çalışma; **okunacak**)
- `[ ]` Biriken mesajları boşaltma (Domingues ve ark.)
- `[ ]` Tıbbi IoT bağlamında MQTT çalışmaları
- `[ ]` MQTT 5 standardı: neden kodları, "ne kadar sonra gel" bilgisini taşıyan bir alanın bulunmadığı (standart metninden doğrulanacak)

## 3. Yöntem

### 3.1 Sistem mimarisi

*Durum: yazılabilir, şekil gerekli.*

Ortam iki katmandan oluşur: uç cihazları temsil eden bir simülatör ve merkezi bir MQTT 5 sunucusu. İkisi de aynı bilgisayarda, ayrı Docker konteynerlerinde, aynı Docker ağı içinde çalışır. Sunucu Eclipse Mosquitto 2.1.2'dir, 2 CPU ve 2 GB bellekle sınırlanmıştır; simülatör 4 CPU ve 3 GB ile çalışır. TLS ve kimlik doğrulama kullanılmamıştır. Bağlantı sonrası yük deneylerinde (Bölüm 5.5-5.7) üçüncü bir bileşen eklenir: tüm cihaz mesajlarına abone olan ve gecikmeyi ölçen ayrı bir toplayıcı konteyner (2 CPU, 1 GB). Toplayıcı ayrıca cihazların durum eşitleme isteklerine yanıt verir. Aradaki bir kenar geçidi (edge gateway) katmanı kapsam dışıdır.

`[Şekil 1: mimari şema]`

### 3.2 Sanal cihaz ve yeniden bağlanma kuralları

*Durum: yazılabilir.*

Her sanal cihaz, MQTTnet 5.2 ile MQTT 5 üzerinden sunucuya bağlanan bir asenkron görevdir (kalp atışı 10 sn, bağlantı zaman aşımı 5 sn, kütüphanenin otomatik yeniden bağlanması kapalı). Bağlantı koptuğunda cihaz bir bekleme kuralına göre bekler ve yeniden dener. Her cihazın rastgele sayı üreteci, deney tekrarlanabilsin diye cihaz numarası ve deney tohumundan türetilir. Üç kural karşılaştırılmıştır:

| Kural | Tanım |
|---|---|
| Sabit | Her seferinde 1 sn |
| Üstel | `min(30, 1 × 2^(n−1))` sn, rastgelelik yok |
| Rastgeleli üstel | 1. bekleme U(0, 5) sn [1]; sonrakiler `W = min(30, 1 × 2^(n−1))` olmak üzere U(0,5·W, W) [2] |

Rastgeleli üstel kural, [1] ve [2]'nin birleşimidir; iki standardın birebir uygulaması değildir.

**İş yükü ve bağlantı sonrası politikalar (Bölüm 5.5-5.7).** Her cihaz 2 sn'de bir 128 baytlık bir vital bulgu mesajı ve 10 sn'de bir 128 baytlık bir alarm mesajı üretir (QoS 1; mesajın planlanan gönderim anı önceden belirlidir). Bağlantı yokken üretilen mesajlar cihazda bellek içi kuyrukta birikir. Cihaz **yeniden bağlandığında** (ilk açılışta değil) bir *bağlantı sonrası politika* uygular: sunucudan 4.096 baytlık güncel durumu ister (eşitleme, MQTT istek/yanıt) ve birikenleri gönderir. Üç politika karşılaştırılmıştır:

| Politika | Tanım |
|---|---|
| B0 (referans) | Biriken atılır, eşitleme yok. Yalnızca referans olarak kullanılır; veriyi attığı için pratik bir politika değildir |
| B1 (hemen) | Bağlanır bağlanmaz eşitleme, ardından birikenin tamamı peş peşe |
| B2 (ayrık) | Bağlandıktan sonra U(0, W) sn bekle, eşitle, birikeni *a* ms aralıkla gönder. W = 30 sn ve a = 200 ms (varsayılan); Bölüm 5.7'de (W, a) = (60 sn, 1 sn) ve (90 sn, 500 ms) de denenmiştir |

### 3.3 Kesinti senaryosu

*Durum: yazılabilir.*

Cihazlar 20 sn içinde yayılarak bağlanır (başlangıç fırtınası olmasın diye). Başlangıçtan 40 sn sonra sunucu konteyneri durdurulur (temiz kesinti: bağlantılar sonlanır, cihazlar kopmayı hemen öğrenir), 60 sn sonra yeniden başlatılır ve 120 sn daha izlenir. Sessiz kesinti (ağın sessizce kopması) kapsam dışıdır.

Bağlantı sonrası yük deneylerinde (Bölüm 5.5-5.7) zaman çizelgesi şöyledir: ilk bağlanma 20 sn'ye yayılır, sunucu 40. saniyede durdurulur, 60 sn kapalı kalır, açıldıktan sonra 90 sn izlenir (Bölüm 5.7'de uzun pencereli politikaların boşaltması bitebilsin diye 180 sn).

### 3.4 Ölçütler

*Durum: tanımlar `scripts/ozet.ps1` başında.*

Toparlanma süresi (TTR): sunucunun başlatıldığı andan bağlı cihazların %50, %95 ve %100'üne ulaşılana kadar geçen süre (çözünürlük 1 sn). Kesintideki ve sonrasındaki tepe bağlantı denemesi/sn. Sunucu açıkken başarısız bağlantı sayısı. Sunucu ve simülatör CPU'su (~0,7 Hz örnekleme). Bağlantı sonrası yük deneylerinde: (i) **canlı alarm gecikmesi**: toplayıcının mesajı aldığı an eksi mesajın *planlanan* gönderim anı (açık döngü, saniyelik p99); *taban* kesinti öncesi saniyelik p99'ların medyanı, *tepe* sunucu açıldıktan sonraki saniyelik p99'un en büyüğü, ayrıca p99'un 100 ms'yi aştığı saniye sayısı; (ii) **boşaltma bitişi**: sunucunun açıldığı andan son biriken mesajın toplayıcıya ulaşmasına kadar geçen süre; (iii) **eşitleme süresi** (istek-yanıt, p50 ve p99); (iv) sunucu CPU tepesi; (v) kayıp ve tekrar sayısı (mesaj numaralarıyla). Gecikme, iki konteynerin ortak çekirdek saati (CLOCK_MONOTONIC) ile ölçülür (Bölüm 4.3). Her hücre 3 tekrarla koşulmuş, **medyan ve en küçük-en büyük aralık** raporlanmıştır; güven aralığı iddiası yoktur.

### 3.5 Deney ortamı

*Durum: `00_ortam.md`'den tablo taşınacak.* `[ ]`

### 3.6 Kayıt ve tekrarlanabilirlik

*Durum: yazılabilir.* Her koşu bir girdi dosyası (`girdi.json`), saniyelik ham çıktı (CSV), kesinti zamanları ve kod sürümü (git commit) ile kayıt defterine işlenir. Özet sayılar elle değil betikle üretilir. Hatalı koşular silinmez, geçersiz olarak işaretlenir.

## 4. Ölçüm Ortamının Doğrulanması

*Durum: malzeme hazır (günlük `04`, Bulgu 1 ve 5). Bu bölüm çalışmanın güvenilirlik göstergesi.*

- **4.1 Zaman aşımı hatası.** `[ ]` MQTTnet `WithTimeout`, yanıt vermeyen sunucuya TCP bağlantı kurulumunu sınırlamıyordu; cihazlar bekleme kuralı hiç devreye girmeden tek bir asılı denemede kaldı. Kesinti sırasındaki deneme sayısı incelenince fark edildi. (koşular: `adim3_*_v1`)
- **4.2 Ad çözümlemesi artefaktı.** `[ ]` Cihazlar sunucuyu adıyla çağırdığında 5.000 cihazın toparlanması 27-69 sn sürdü, IP ile 2,1 sn. Sunucu MAC adresi ve çekirdek kuyruk taşması elenmiştir. (koşular: `v2`, `v2b`, `v3mac`, `v4ip`). Mekanizma doğrudan gözlenmedi.
- **4.3 Saat sıçraması.** Bağlantı sonrası yük deneylerinin ilk pilotunda canlı alarm gecikmesi bazı saniyelerde negatif çıktı (−2,3 sn, −4,7 sn); bir mesajın planlanan andan önce alınması imkânsız olduğundan bu bir ölçüm hatasıydı. İnceleme iki neden gösterdi. Birincisi, cihaz zamanlayıcısı (`Task.Delay`) mesaj zamanından önce uyanıyor ve kod uyanma sonrası kontrol yapmıyordu; kontrol eklendi. İkincisi, **WSL2 sanal makinesinin duvar saati yaklaşık her 30 sn'de bir ani sıçrıyor**: kayıtlı saat bekçisi, örneğin +3.011 ms ve 100 ms sonra −3.487 ms sıçramaları yakaladı. Kalıcı çözüm olarak planlanan ve alınan anlar tek yönlü saatle (CLOCK_MONOTONIC) ölçüldü; 1.560 mesajlık doğrulama koşusunda, iki saat sıçramasına rağmen gecikme 0-20 ms çıktı. Duvar saatiyle yapılan konteynerler arası gecikme ölçümleri bu ortamda bozulabilir. (günlük `06`, Bulgu 1)
- **4.4 Çıkarım.** `[ ]` Toplam "bağlandı" sayısı yetmez, kesinti sırasındaki deneme sayısı da doğrulanmalı; ölçüm ortamının kendisi "fırtına"yı üretebilir ya da gizleyebilir.

## 5. Bulgular

*Durum: 5.5-5.7 yazıldı (Adım 5, 5b, 5c; tablolar `ozet_adim5_v6/v7/v8_hucreler.csv`'den). 5.1-5.3 (bekleme kuralı karşılaştırması, `05_adim4_matris_v5.md` ve `ozet_adim4*`) ve 5.4 (TLS) henüz yazılmadı.*

- `[ ]` 5.1 Üç bekleme kuralının deneme dağılımı (Şekil)
- `[ ]` 5.2 Cihaz sayısına göre toparlanma süresi (Şekil, medyan ve aralık)
- `[ ]` 5.3 Sunucu yükü
- `[ ]` 5.4 TLS'li durum (pilot sonrası)
- 5.5-5.7: aşağıda (Adım 5, 5b, 5c).

### 5.5 Bağlantı sonrası yük, N = 5.000 (sürüm v6)

A = {üstel, rastgeleli üstel} × B = {B0, B1, B2} = 6 hücre, her hücre 3 tekrar (18 koşu; tohum 1-3). Kesinti öncesi canlı alarm gecikmesi (taban) tüm koşularda 26-37 ms'dir.

**Tablo 1.** N = 5.000, sunucu açıldıktan sonra canlı alarm gecikmesi (medyan [en küçük-en büyük]). Kaynak: `results/ozet_adim5_v6_hucreler.csv`.

| A | B | Alarm p99 tepe (ms) | >100 ms süre (sn) | Boşaltma bitişi (sn) | Sunucu CPU tepe (%) |
|---|---|---|---|---|---|
| üstel | B0 | 47 [44-51] | 0 | - | 37 [33-38] |
| üstel | B1 | 9.158 [8.821-10.413] | 19 [18-23] | 42,6 [41,6-45,0] | 98 [98-98] |
| üstel | B2 | 143 [135-145] | 17 [16-19] | 65,6 [65,2-65,8] | 58 [57-64] |
| rastgeleli | B0 | 60 [47-70] | 0 | - | 21 [19-24] |
| rastgeleli | B1 | 51 [49-66] | 0 | 28,9 [28,2-29,5] | 85 [80-87] |
| rastgeleli | B2 | 79 [72-112] | 0 [0-2] | 63,7 [62,2-65,6] | 59 [58-61] |

`[Şekil: sekil_alarm_zaman_n5000.png, sekil_alarm_tepe_hucre.png]`

Üstel kuralda B1 ile 5.000 cihazın birkaç saniye içinde geri dönüp aynı anda eşitleme istemesi ve birikeni boşaltması, canlı alarm gecikmesinin p99'unu yaklaşık 30 ms'den yaklaşık 9 sn'ye çıkarır (taban değerin yaklaşık 300 katı) ve p99 yaklaşık 19 sn boyunca 100 ms'nin üstünde kalır; sunucunun tek çekirdeği doyar (CPU tepesi %98) ve eşitleme isteklerinin p99'u 8 sn'ye çıkar. Aynı kuralla B0'da tepe 47 ms'dir, yani etki bağlanmanın kendisinden değil bağlantı sonrası işten gelir. Dönüşün rastgele dağıtılması (rastgeleli üstel kural) aynı B1 politikasında tepeyi 51 ms'ye indirir ve ek bir politikayı gereksiz kılar. Üstel kuralda B2 tepeyi %98 düşürür (9.158 → 143 ms), bedeli boşaltmanın 23 sn daha geç bitmesidir; rastgeleli kuralda B2 gereksizdir ve küçük bir kötüleşme yaratır (79 ms). B0'ın düşük gecikmesi, birikenin atılmasıyla sağlanır (üretilen mesajın %41-48'i).

*Kayıp ve tekrar:* tekrar 18 koşunun hepsinde 0; kayıp 16 koşuda 0, rastgeleli/B1'in iki koşusunda (tohum 1 ve 3) 1 mesajdır. Neden doğrulanmamıştır (Bölüm 6).

### 5.6 Ölçek: N = 10.000 (sürüm v7)

Aynı düzenek N = 10.000 ile, A × {B1, B2} = 4 hücre, her hücre 3 tekrar (12 koşu). Başlangıç yayılımı 20 sn'de sabit tutulmuştur (varış hızı iki katına çıkar). Taban 51-59 ms'dir.

**Tablo 2.** N = 10.000 (medyan [en küçük-en büyük]). Kaynak: `results/ozet_adim5_v7_hucreler.csv`.

| A | B | Alarm p99 tepe (ms) | >100 ms süre (sn) | Boşaltma bitişi (sn) | Sunucu CPU tepe (%) |
|---|---|---|---|---|---|
| üstel | B1 | 19.741 [19.109-20.209] | 45 [44-48] | 61,3 [61,2-63,7] | 91 [89-92] |
| üstel | B2 | 9.259 [8.672-11.270] | 51 [50-54] | 79,7 [77,2-80,9] | 77 [75-79] |
| rastgeleli | B1 | 6.157 [4.582-6.288] | 28 [25-28] | 34,4 [30,0-34,5] | 89 [89-91] |
| rastgeleli | B2 | 7.324 [5.707-7.387] | 47 [42-48] | 70,9 [67,6-72,1] | 78 [76-78] |

N = 5.000'de yeterli olan çözümlerin hiçbiri N = 10.000'de yeterli kalmaz: rastgeleli/B1'in tepesi 51 ms'den 6.157 ms'ye (yaklaşık 120 kat), rastgeleli/B2'nin tepesi 79 ms'den 7.324 ms'ye çıkar. Sunucu CPU'su B1'de %89-91'e dayanır. Dönüşü dağıtmak aynı B1 ile tepeyi yine 3,2 kat düşürür (19.741 → 6.157 ms), ama tek başına yetmez. B2 (30 sn / 200 ms) üstel kuralda tepeyi yarıya indirir (%53), rastgeleli kuralda ise düşürmez (7.324 ms, B1'e göre %19 daha yüksek) ve boşaltmayı iki katına uzatır. Taban gecikmenin 5.000'deki yaklaşık 30 ms'den yaklaşık 55 ms'ye çıkması, simülatörün 10.000 cihazı koşturmasının da gecikmeye karıştığına işaret edebilir; doğrulanmamıştır.

### 5.7 B2 parametrelerinin N'ye göre ayarlanması (sürüm v8)

Bölüm 5.6'daki sorunun yeniden bağlanmadan mı, bağlantı sonrası işten mi geldiğini ayırmak için N = 10.000'de rastgeleli kuralla B0 kontrol olarak koşulmuş; ayrıca B2'nin pencere ve aralığı büyütülmüştür (toplam boşaltma hızını düşürmek için). Üç hücre, 3 tekrar (9 koşu), izleme 180 sn. Taban 51-54 ms.

**Tablo 3.** N = 10.000, rastgeleli üstel kural (medyan [en küçük-en büyük]). Kaynak: `results/ozet_adim5_v8_hucreler.csv`.

| Politika | Alarm p99 tepe (ms) | >100 ms süre (sn) | Boşaltma bitişi (sn) | Sunucu CPU tepe (%) |
|---|---|---|---|---|
| B0 (kontrol) | 90 [82-112] | 0 [0-1] | - | 37 [36-37] |
| B2, 60 sn / 1 sn | 237 [213-355] | 45 [38-47] | 137,7 [134,9-138,1] | 66 [63-67] |
| B2, 90 sn / 500 ms | 169 [154-198] | 21 [13-23] | 140,2 [139,9-140,4] | 60 [59-62] |

`[Şekil: sekil_alarm_zaman_n10000.png, sekil_alarm_denge_n10000.png]`

B0'ın tepesinin 82-112 ms olması, N = 10.000'de yaklaşık 6 sn'lik tepenin yeniden bağlanmanın kendisinden değil bağlantı sonrası işten geldiğini gösterir. Büyütülmüş B2 ayarlarının her ikisi tepeyi yaklaşık 7 sn'den 150-350 ms'ye indirir (B2 30/200'e göre %97-98), sunucu CPU'sunu %60-66'ya düşürür ve eşitleme p99'unu yaklaşık 7 sn'den 94-143 ms'ye indirir. Bedel, boşaltmanın yaklaşık 140 sn'de bitmesidir (B1'de 34 sn; yaklaşık 4 kat); kesintide biriken veri, sunucu açıldıktan yaklaşık 2,3 dk sonra tamamlanır. Hiçbir ayar tabana (yaklaşık 52 ms) inmez; tepe taban değerin 3-7 katıdır. İki ayarda pencere ve aralık birlikte değiştiğinden, hangisinin etkili olduğu bu verilerle ayrılamaz. Bu sonuç, B2'nin parametrelerinin cihaz sayısına göre ayarlanması gerektiğine işaret eder, ancak iki noktaya dayanır ve bir ölçekleme yasası değildir. B1 ve B2 30/200 karşılaştırma değerleri Bölüm 5.6'dan alınmıştır ve aynı blokta koşulmamıştır.

*Kayıp ve tekrar:* tekrar 9 koşunun hepsinde 0; bir koşuda (90 sn / 500 ms, tohum 3) 2 mesaj kayıptır.

*Ön gözlem (yalnızca 100 cihaz, tek koşu, günlük `03`; Bölüm 5.1 için):* rastgeleli üstel kural tepe deneme sayısını 100'den 39'a düşürdü, toparlanmayı 2,5 sn'den 16 sn'ye uzattı. Bu, [1] ve [2]'deki bilinen eğilimle uyumlu. **Nihai bulgu olarak kullanılmayacak.**

## 6. Tartışma ve Sınırlar

**Bağlantı sonrası yük deneylerine özgü sınırlar.** (i) Mosquitto tek iş parçacıklıdır; bulgular çok çekirdekli bir sunucuya genellenemez. (ii) Toplayıcı tek istemcidir ve onun yavaşlaması gecikmeye karışmış olabilir; toplayıcının işlem yükü ölçülmemiştir. (iii) N = 10.000'de taban gecikmenin yükselmesi simülatörün yükünü düşündürür; doğrulanmamıştır. (iv) 39 koşunun 6'sında 1 ile 132 arasında mesaj, toplayıcıya ulaşmamıştır (toplam yaklaşık 540 bin ile 1,6 milyon mesaj içinde); nedeni açıklanmamıştır. Olası ama doğrulanmamış açıklama: sunucu durdurulurken sunucunun onaylayıp toplayıcıya henüz iletmediği, bellek içi mesajların kaybı. (v) B2 yalnızca iki ek ayarla sınanmış, ayar uzayı taranmamıştır. (vi) Biriken vital ve alarm mesajları aynı sırayla boşaltılır; alarmları önceliklendirmek gelecek çalışmadır. (vii) İş yükü (mesaj boyutu, vital ve alarm sıklığı, eşitleme boyutu) varsayımsaldır, klinik gerçekliği temsil etmez.

`[ ]` Sınırlar (günlüklerden toplanacak): tek bilgisayar, sanal cihaz, sıfır gecikmeli ağ, TLS'siz, tek sunucu, tek kesinti türü (temiz), tekrar sayısı, CPU örnekleme sıklığı, çözünürlük 1 sn, mekanizmaların bir kısmı doğrudan gözlenmedi.

## 7. Sonuç ve Gelecek Çalışma

`[ ]` Sonuç (Adım 5 için ön metin, Bölüm 5.1-5.4 tamamlanınca birleştirilecek): bu ortamda, aynı anda dönen çok sayıda cihazın bağlantı sonrası işi (eşitleme ve birikenin boşaltılması) canlı alarm gecikmesini bağlanmanın kendisinden çok daha fazla artırmıştır; dönüşü dağıtmak bu etkiyi N = 5.000'de ortadan kaldırmış, N = 10.000'de yetmemiştir; boşaltmayı zamana yayan B2 politikası ancak parametreleri cihaz sayısına göre büyütüldüğünde N = 10.000'de gecikmeyi yüzlerce milisaniyeye indirmiş, bedel olarak boşaltmanın yaklaşık 4 kat uzamasına yol açmıştır. Gelecek çalışma adayları: sessiz kesinti, TLS maliyeti, kenar geçidi katmanı, sunucunun "ne kadar sonra gel" bilgisi vermesi, alan adı çözümlemesinin toplu yeniden bağlanmadaki rolü.

## Kaynakça

Bkz. `kaynakca.md`. Metinde geçenler:

[1] I. Fette ve A. Melnikov, "The WebSocket Protocol," RFC 6455, IETF, Aralık 2011. `[künye kontrol]`

[2] C. Jennings, R. Mahy ve F. Audet, "Managing Client-Initiated Connections in the Session Initiation Protocol (SIP)," RFC 5626, IETF, Ekim 2009. `[künye kontrol]`

## Ekler

- EK-A: Deney kayıt defteri (`results/deney_kayitlari.csv`)
- EK-B: Koşu adları ve girdi dosyaları
- EK-C: Kod deposu bağlantısı (github.com/mertbayir/mqtt-analysis-project)
