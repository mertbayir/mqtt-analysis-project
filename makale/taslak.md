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

Çalışma tek bir bilgisayarda, sanal cihazlarla yapılmıştır; deneylerin çoğu TLS'sizdir, TLS yalnızca bağlanma aşamasında denenmiştir (Bölüm 5.4); sonuçlar bu ortama göreceli olarak raporlanır, mutlak kapasite iddiası taşımaz. Tıbbi çerçeve bir senaryo olarak kullanılmıştır, klinik güvenlik iddiası yoktur.

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

Ortam iki katmandan oluşur: uç cihazları temsil eden bir simülatör ve merkezi bir MQTT 5 sunucusu. İkisi de aynı bilgisayarda, ayrı Docker konteynerlerinde, aynı Docker ağı içinde çalışır. Sunucu Eclipse Mosquitto 2.1.2'dir, 2 CPU ve 2 GB bellekle sınırlanmıştır; simülatör 4 CPU ve 3 GB ile çalışır. Kimlik doğrulama kullanılmamıştır; TLS yalnızca Bölüm 5.4'te açıktır (cihazlar 8883 numaralı portta TLS ile bağlanır), diğer deneyler TLS'sizdir. Bağlantı sonrası yük deneylerinde (Bölüm 5.5-5.7) üçüncü bir bileşen eklenir: tüm cihaz mesajlarına abone olan ve gecikmeyi ölçen ayrı bir toplayıcı konteyner (2 CPU, 1 GB). Toplayıcı ayrıca cihazların durum eşitleme isteklerine yanıt verir. Aradaki bir kenar geçidi (edge gateway) katmanı kapsam dışıdır.

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

*Durum: 5.1-5.3 (Adım 4, sürüm v5), 5.4 (Adım 6, TLS, sürüm t1), 5.5-5.7 (Adım 5, 5b, 5c; sürümler v6, v7, v8) 5.8 (Adım 6b, TLS'te bağlantı sonrası yük, sürüm tls6) ve 5.9 (Adım 7, ikinci sunucu EMQX, sürümler e1, e2) yazıldı.*

Bölüm 5.1-5.3'te sunucu açılışında yalnızca *bağlanma* aşaması ölçülür (iş yükü yok); 5.5-5.7 bağlantı sonrası işi ekler. Hipotezler (H1-H4) koşulardan önce günlüğe yazılmıştır (`05_adim4_matris_v5.md`). Tüm koşular IP ile bağlanır (Bölüm 4.2). Tasarım: kural (sabit, üstel, rastgeleli üstel) × N (2.000, 5.000) = 6 hücre, her hücre 3 tekrar (tohum 1-3), bloklar içinde rastgele sırayla (18 koşu, sürüm v5); kesinti temiz, 60 sn.

### 5.1 Üç bekleme kuralının deneme dağılımı

**Tablo 1.** Kesinti sırasındaki tepe bağlantı denemesi/sn ve koşu boyunca toplam deneme sayısı (medyan [en küçük-en büyük]). Kaynak: `results/ozet_adim4_hucreler.csv`, `ozet_adim4_kosular.csv`.

| N | Kural | Kesintide tepe deneme/sn | Koşu boyunca toplam deneme |
|---|---|---|---|
| 2.000 | sabit | 2.000 [2.000-2.000] | 22.069 [22.005-22.083] |
| 2.000 | üstel | 2.000 [2.000-2.000] | 10.000 [10.000-10.000] |
| 2.000 | rastgeleli üstel | 484 [475-521] | 10.060 [10.052-10.101] |
| 5.000 | sabit | 5.000 [4.641-5.000] | 53.412 [53.119-55.167] |
| 5.000 | üstel | 5.000 [5.000-5.000] | 25.000 [25.000-25.000] |
| 5.000 | rastgeleli üstel | 1.263 [1.219-1.285] | 25.139 [25.087-25.149] |

`[Şekil: sekil_deneme_n5000_v5.png (N = 5.000, tohum 2), sekil_matris_ozet.png]`

Sabit ve üstel kuralda kesinti anında tüm cihazlar (tepe = N) aynı saniyede deneme yapar; rastgeleli üstel kural tepeyi N'nin yaklaşık dörtte birine indirir (N = 2.000'de 484, N = 5.000'de 1.263). Üstel geri çekilme koşu boyunca toplam deneme sayısını sabit kurala göre yaklaşık 2,2-2,1 kat azaltır (22.069 → 10.000; 53.412 → 25.000); rastgelelik toplamı değiştirmez, yalnızca denemeleri zamana yayar (10.060 ve 25.139). Bu, [1] ve [2]'de bilinen eğilimin yeniden üretilmesidir ve ölçüm ortamının beklenen davranışı gösterdiğinin sağlamasıdır; yeni bir bulgu değildir.

### 5.2 Toparlanma süresi ve cihaz sayısı

**Tablo 2.** Sunucu açıldıktan sonra bağlı cihazların %50'sine ve %100'üne ulaşma süresi (TTR, sn; çözünürlük 1 sn; medyan [en küçük-en büyük]) ve sunucu açıldıktan sonra başarısız bağlantı denemesi sayısı. Kaynak: aynı dosyalar.

| N | Kural | TTR %50 | TTR %100 | Sunucu açıkken başarısız bağlantı |
|---|---|---|---|---|
| 2.000 | sabit | 5,3 [5,0-5,3] | 6,3 [6,0-6,3] | 2.065 [2.052-2.169] |
| 2.000 | üstel | 24,2 [24,1-24,4] | 25,2 [25,1-25,4] | 0 [0-0] |
| 2.000 | rastgeleli üstel | 12,3 [12,3-13,0] | 34,3 [34,3-35,0] | 243 [217-273] |
| 5.000 | sabit | 5,1 [0,9-5,2] | 7,1 [2,9-7,2] | 5.098 [1-5.259] |
| 5.000 | üstel | 24,1 [24,1-24,2] | 25,1 [25,1-25,2] | 0 [0-0] |
| 5.000 | rastgeleli üstel | 12,1 [12,1-12,1] | 35,1 [32,1-35,1] | 576 [253-644] |

Rastgeleli üstel kural, tepe denemeyi düşürmenin bedeli olarak toparlanmanın son kısmını uzatır: TTR %100, sabit kuralın yaklaşık 5 katı (34-35 sn'ye karşı 6-7 sn) ve üstelin yaklaşık 1,4 katıdır; buna karşılık TTR %50 daha kısadır (12 sn; üstelde 24 sn), yani uzayan yalnızca son kısımdır. N = 2.000 ile 5.000 arasında aynı kural için TTR %100 medyanları en çok 1 sn farklıdır (sabit 6,3 → 7,1; üstel 25,2 → 25,1; rastgeleli 34,3 → 35,1): bu iki N arasında bir devrilme noktası görülmemiştir. Yalnızca iki N denendiği için bu sınırlı bir kanıttır.

**Üstel kuralda TTR bir takvim sonucudur.** Üstel kuralın TTR %100 değeri üç tohum ve iki N için 25,1-25,4 sn'dir ve elle hesaplanan değerle birebir uyuşur: cihazlar kopmadan sonra t = 1, 8, 17, 30, 51 ve 86. saniyelerde dener (bekleme 1, 2, 4, 8, 16 ve 30 sn artı 5 sn bağlantı zaman aşımı); sunucu t ≈ 60,9 sn'de açıldığından 51. saniyedeki deneme başarısız olur, sonraki deneme 86. saniyededir ve 86 − 60,9 = 25,1 sn eder. Dolayısıyla sonuç **kesintinin süresine bağlı bir testere dişi fonksiyondur**: örneğin 45 sn'lik bir kesintide ilk başarılı deneme t = 51 sn'de olurdu, yani TTR yaklaşık 5 sn çıkardı (bu bir hesaptır, ölçülmemiştir). "Üstel kuralın TTR'si 25 sn" ifadesi tek bir kesinti süresine özgüdür; kesinti süresi taraması yapılmamıştır (Bölüm 6).

Sabit kuralda N = 5.000, tohum 1 koşusu (TTR %100 2,9 sn, başarısız bağlantı 1) diğer iki tohumdan (7,1-7,2 sn, yaklaşık 5.100) ayrılır; bu, sunucunun açıldığı anın 6 sn'lik deneme döngüsünün neresine denk geldiğiyle (faz) tutarlıdır ve aykırı olarak çıkarılmamıştır. Mekanizma doğrudan incelenmemiştir.

### 5.3 Sunucu yükü

**Tablo 3.** Sunucu ve simülatör CPU tepesi (Docker `stats` yüzdesi; 100 = bir çekirdek; sunucu sınırı 200, simülatör sınırı 400; örnekleme yaklaşık 0,7 Hz; medyan [en küçük-en büyük]). Kaynak: `results/ozet_adim4_hucreler.csv`, `ozet_adim4_kosular.csv`.

| N | Kural | Sunucu CPU tepe | Simülatör CPU tepe (koşular: en büyük) |
|---|---|---|---|
| 2.000 | sabit | 9,9 [7,7-10,8] | 91,6 |
| 2.000 | üstel | 3,3 [2,1-11,5] | 85,0 |
| 2.000 | rastgeleli üstel | 1,8 [1,8-2,5] | 39,8 |
| 5.000 | sabit | 24,2 [21,0-29,8] | 244,6 |
| 5.000 | üstel | 12,2 [7,8-18,5] | 176,3 |
| 5.000 | rastgeleli üstel | 6,8 [3,7-22,6] | 115,6 |

Bu ortamda (TLS'siz Mosquitto, 2 CPU, N ≤ 5.000) sunucu bağlanma aşamasında zorlanmamıştır: CPU tepesi en çok 29,8'dir (sınır 200). Üstel kuralda sunucu açıldığında 5.000 cihazın tamamı ilk denemede bağlanmıştır (sunucu açıkken başarısız bağlantı 0), yani saniyede yaklaşık 5.000 eşzamanlı bağlantı kurulumu sunucuyu doyurmamıştır. Sabit ve rastgeleli kurallardaki sunucu açıkken başarısız bağlantıların (Tablo 2) nedeni incelenmemiştir. Simülatörün CPU tepesi sabit kuralda N = 5.000'de 244,6'ya (sınırın yaklaşık %61'i) çıkmıştır: yük üreticisi sınıra dayanmamış ama yüksektir.

Bu sonuç iki biçimde yorumlanmalıdır. Birincisi, rastgeleli kuralın faydası bu TLS'siz ortamda ölçülememiştir, yalnızca bedeli (TTR %100'ün 6-7 sn'den 34-35 sn'ye çıkması) ölçülmüştür; "rastgele bekleme gereksizdir" sonucu çıkarılmamalıdır, çünkü bulgu tek sunuculu, sunucunun yetiştiği bir ortama özgüdür; TLS eklendiğinde yön tersine döner (Bölüm 5.4). İkincisi, bağlanma aşamasının kendisi bu ölçekte sunucuyu zorlamazken, bağlantı sonrası iş (eşitleme ve biriken mesajların boşaltılması) zorlamıştır (Bölüm 5.5-5.7); bu çalışmanın asıl sorusu budur.

### 5.4 TLS ile toplu yeniden bağlanma (sürüm t1)

Bölüm 5.1-5.3'teki aynı tasarım (kural × N = 2.000 ve 5.000 × 3 tekrar, 18 koşu, temiz kesinti 60 sn), tek fark cihazların sunucuya TLS ile bağlanmasıdır (RSA-2048 anahtarlı test sertifikası; sertifika doğrulanmaz, yük testi; TLS sürümü ve şifre takımı ölçülmemiştir). İzleme 150 sn'dir (TLS'siz deneylerde 90 sn). Hipotezler (HT1-HT6) koşulardan önce günlüğe yazılmıştır (`10_adim6_tls.md`); 5.000 cihazlık tek bir pilot koşunun sonucu yazım sırasında biliniyordu.

**Tablo 4.** TLS ile (t1) medyan [en küçük-en büyük]; parantez içinde TLS'siz (v5) medyan (Tablo 2 ve 3). Kaynak: `scripts/tekrar_ozet.py t1` çıktısı, `results/adim3_*_t1.*`.

| N | Kural | TTR %50 (sn) | TTR %100 (sn) | Sunucu açıkken başarısız bağlantı | Sunucu CPU tepe |
|---|---|---|---|---|---|
| 2.000 | sabit | 2,1 [1,2-2,2] (5,3) | 3,2 [3,1-3,2] (6,3) | 670 [64-700] (2.065) | 78,4 [70,7-79,5] (9,9) |
| 2.000 | üstel | 26,2 [26,1-27,1] (24,2) | 61,2 [60,1-63,1] (25,2) | 325 [105-571] (0) | 33,9 [19,8-36,8] (3,3) |
| 2.000 | rastgeleli üstel | 12,0 [11,2-12,1] (12,3) | 28,1 [27,2-30,0] (34,3) | 0 [0-26] (243) | 13,0 [12,5-13,6] (1,8) |
| 5.000 | sabit | 3,9 [3,0-4,0] (5,1) | 9,9 [8,0-11,0] (7,1) | 1.128 [321-1.780] (5.098) | 85,4 [82,0-89,0] (24,2) |
| 5.000 | üstel | 28,0 [26,0-28,9] (24,1) | 62,9 [62,0-95,0] (25,1) | 1.836 [1.119-1.919] (0) | 63,7 [63,5-89,8] (12,2) |
| 5.000 | rastgeleli üstel | 10,9 [10,9-11,9] (12,1) | 28,9 [28,9-29,9] (35,1) | 0 [0-0] (576) | 30,0 [29,7-31,9] (6,8) |

`[Şekil: sekil_tls_toparlanma_n5000.png]`

TLS, bağlanma aşamasını sunucu için gerçek bir yüke dönüştürür: aynı kural ve N için sunucu CPU tepesi 3,5-5,2 kat yükselir (N = 5.000'de sabit 24,2 → 85,4; üstel 12,2 → 63,7; rastgeleli 6,8 → 30,0) ve üstel kuralda sunucu açıkken başarısız bağlantı, TLS'siz durumdaki 0 yerine N = 5.000'de 1.119-1.919'a çıkar. Üstel kuralda TTR %100 25 sn'den 60-63 sn'ye (N = 2.000 ve 5.000'de) çıkar; etki N = 2.000'de de vardır. Bu süreler bir takvim sonucu olarak yorumlanabilir: 86. saniyedeki ilk deneme dalgasının el sıkışmaları sunucuyu aşınca zaman aşımına uğrayan cihazlar, 5 sn sonra 30 sn'lik üst sınır beklemesiyle t = 121 sn'de yeniden dener (121 − 60,9 = 60,1 sn); bir koşuda (N = 5.000, tohum 3) üçüncü dalga t = 156 sn'de gerçekleşir (156 − 60,9 = 95,1 sn) ve ölçülen değer 95,0 sn'dir. Şekilde (N = 5.000, tohum 2) bu iki dalga görülür: ilk dalgada yaklaşık 3.100 cihaz bağlanır, kalanlar yaklaşık 60. saniyedeki ikinci dalgada. Sayısal örtüşme güçlüdür, ancak ikinci dalganın nedeni doğrudan gözlenmemiş, hesapla tutarlıdır.

**Rastgeleli kuralın yönü TLS ile tersine döner.** TLS'siz ortamda rastgeleli kural yalnızca bedel getirmişti (TTR %100 üstelden yaklaşık 1,4 kat uzun, Bölüm 5.2). TLS'te ise TTR %100'ü yarıdan fazla kısaltır (N = 5.000'de 28,9 sn; üstel 62,9 sn), sunucu açıkken başarısız bağlantıyı N = 5.000'de sıfırlar (üstel 1.836) ve sunucu CPU tepesini yarıya indirir (30,0; üstel 63,7). Yani Bölüm 5.3'te "ölçülememiş" olan fayda, sunucu zorlandığında görünür hâle gelir. Sabit kural TLS'te sunucuyu en çok yoran kuraldır (N = 5.000'de CPU tepesi 82-89, tek çekirdeğin yaklaşık %85'i); TTR %100 yine kısadır (9,9 sn), çünkü yeniden deneme sıklığı yüksektir. Sabit kuralın N = 2.000'de TLS'te TLS'siz değerden hızlı çıkması (3,2 sn'ye karşı 6,3 sn) açıklanamamıştır; faz etkisi olabilir, ölçülmemiştir.

*Sınırlar:* TLS'siz karşılaştırma Bölüm 5.1-5.3'ten alınmıştır ve aynı blokta koşulmamıştır. İstemci tarafı el sıkışma maliyeti ayrılamamıştır; simülatörün CPU tepesi bazı koşularda sınırın yaklaşık %80'ine ulaşmıştır (en çok 320, sınır 400), dolayısıyla gecikmenin bir kısmı istemciden gelmiş olabilir. Sertifika doğrulama maliyeti, TLS sürümü ve oturum devam ettirmenin etkisi ölçülmemiştir. Üstel/N = 5.000'de tek koşu 95 sn ile aykırı görünür ve çıkarılmamıştır. Bağlantı sonrası iş TLS'te denenmemiştir.

### 5.5 Bağlantı sonrası yük, N = 5.000 (sürüm v6)

A = {üstel, rastgeleli üstel} × B = {B0, B1, B2} = 6 hücre, her hücre 3 tekrar (18 koşu; tohum 1-3). Kesinti öncesi canlı alarm gecikmesi (taban) tüm koşularda 26-37 ms'dir.

**Tablo 5.** N = 5.000, sunucu açıldıktan sonra canlı alarm gecikmesi (medyan [en küçük-en büyük]). Kaynak: `results/ozet_adim5_v6_hucreler.csv`.

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

**Tablo 6.** N = 10.000 (medyan [en küçük-en büyük]). Kaynak: `results/ozet_adim5_v7_hucreler.csv`.

| A | B | Alarm p99 tepe (ms) | >100 ms süre (sn) | Boşaltma bitişi (sn) | Sunucu CPU tepe (%) |
|---|---|---|---|---|---|
| üstel | B1 | 19.741 [19.109-20.209] | 45 [44-48] | 61,3 [61,2-63,7] | 91 [89-92] |
| üstel | B2 | 9.259 [8.672-11.270] | 51 [50-54] | 79,7 [77,2-80,9] | 77 [75-79] |
| rastgeleli | B1 | 6.157 [4.582-6.288] | 28 [25-28] | 34,4 [30,0-34,5] | 89 [89-91] |
| rastgeleli | B2 | 7.324 [5.707-7.387] | 47 [42-48] | 70,9 [67,6-72,1] | 78 [76-78] |

N = 5.000'de yeterli olan çözümlerin hiçbiri N = 10.000'de yeterli kalmaz: rastgeleli/B1'in tepesi 51 ms'den 6.157 ms'ye (yaklaşık 120 kat), rastgeleli/B2'nin tepesi 79 ms'den 7.324 ms'ye çıkar. Sunucu CPU'su B1'de %89-91'e dayanır. Dönüşü dağıtmak aynı B1 ile tepeyi yine 3,2 kat düşürür (19.741 → 6.157 ms), ama tek başına yetmez. B2 (30 sn / 200 ms) üstel kuralda tepeyi yarıya indirir (%53), rastgeleli kuralda ise düşürmez (7.324 ms, B1'e göre %19 daha yüksek) ve boşaltmayı iki katına uzatır. Taban gecikmenin 5.000'deki yaklaşık 30 ms'den yaklaşık 55 ms'ye çıkması, simülatörün 10.000 cihazı koşturmasının da gecikmeye karıştığına işaret edebilir; doğrulanmamıştır.

### 5.7 B2 parametrelerinin N'ye göre ayarlanması (sürüm v8)

Bölüm 5.6'daki sorunun yeniden bağlanmadan mı, bağlantı sonrası işten mi geldiğini ayırmak için N = 10.000'de rastgeleli kuralla B0 kontrol olarak koşulmuş; ayrıca B2'nin pencere ve aralığı büyütülmüştür (toplam boşaltma hızını düşürmek için). Üç hücre, 3 tekrar (9 koşu), izleme 180 sn. Taban 51-54 ms.

**Tablo 7.** N = 10.000, rastgeleli üstel kural (medyan [en küçük-en büyük]). Kaynak: `results/ozet_adim5_v8_hucreler.csv`.

| Politika | Alarm p99 tepe (ms) | >100 ms süre (sn) | Boşaltma bitişi (sn) | Sunucu CPU tepe (%) |
|---|---|---|---|---|
| B0 (kontrol) | 90 [82-112] | 0 [0-1] | - | 37 [36-37] |
| B2, 60 sn / 1 sn | 237 [213-355] | 45 [38-47] | 137,7 [134,9-138,1] | 66 [63-67] |
| B2, 90 sn / 500 ms | 169 [154-198] | 21 [13-23] | 140,2 [139,9-140,4] | 60 [59-62] |

`[Şekil: sekil_alarm_zaman_n10000.png, sekil_alarm_denge_n10000.png]`

B0'ın tepesinin 82-112 ms olması, N = 10.000'de yaklaşık 6 sn'lik tepenin yeniden bağlanmanın kendisinden değil bağlantı sonrası işten geldiğini gösterir. Büyütülmüş B2 ayarlarının her ikisi tepeyi yaklaşık 7 sn'den 150-350 ms'ye indirir (B2 30/200'e göre %97-98), sunucu CPU'sunu %60-66'ya düşürür ve eşitleme p99'unu yaklaşık 7 sn'den 94-143 ms'ye indirir. Bedel, boşaltmanın yaklaşık 140 sn'de bitmesidir (B1'de 34 sn; yaklaşık 4 kat); kesintide biriken veri, sunucu açıldıktan yaklaşık 2,3 dk sonra tamamlanır. Hiçbir ayar tabana (yaklaşık 52 ms) inmez; tepe taban değerin 3-7 katıdır. İki ayarda pencere ve aralık birlikte değiştiğinden, hangisinin etkili olduğu bu verilerle ayrılamaz. Bu sonuç, B2'nin parametrelerinin cihaz sayısına göre ayarlanması gerektiğine işaret eder, ancak iki noktaya dayanır ve bir ölçekleme yasası değildir. B1 ve B2 30/200 karşılaştırma değerleri Bölüm 5.6'dan alınmıştır ve aynı blokta koşulmamıştır.

*Kayıp ve tekrar:* tekrar 9 koşunun hepsinde 0; bir koşuda (90 sn / 500 ms, tohum 3) 2 mesaj kayıptır.


### 5.8 TLS'te bağlantı sonrası yük (sürüm tls6)

Bölüm 5.5'teki düzenek (kural × B0/B1/B2, N = 5.000, 3 tekrar, 18 koşu) cihazların TLS ile bağlanmasıyla yinelenmiştir; toplayıcı TLS'siz bağlanır, izleme 180 sn'dir (TLS'siz deneyde 90 sn). Hipotezler (HF1-HF5) koşulardan önce günlüğe yazılmıştır (`11_adim6b_tls_baglanti_sonrasi.md`); TLS'te bağlanma (Bölüm 5.4) ve TLS'siz bağlantı sonrası (Bölüm 5.5) sonuçları yazım sırasında biliniyordu. Matrisin ilk denemesi oturum kapandığı için yarım kalmış, 13 koşu tamamlanmış, kalan 5 koşu aynı betik ve kodla sonradan koşulmuştur (günlük `11`). Kesinti öncesi taban 27-49 ms'dir.

**Tablo 8.** N = 5.000, TLS ile (tls6), sunucu açıldıktan sonra canlı alarm gecikmesi, medyan [en küçük-en büyük]; parantez içinde TLS'siz (Tablo 5) medyan. Kaynak: `results/ozet_adim5_tls6_hucreler.csv`.

| A | B | Alarm p99 tepe (ms) | >100 ms süre (sn) | Boşaltma bitişi (sn) | Sunucu CPU tepe (%) |
|---|---|---|---|---|---|
| üstel | B0 | 613 [429-689] (47) | 8 [7-8] (0) | - | 85,4 [58,6-96,0] (37) |
| üstel | B1 | 8.234 [6.637-8.915] (9.158) | 30 [30-31] (19) | 69,2 [68,9-70,8] (42,6) | 100,3 [99,5-101,0] (98) |
| üstel | B2 | 907 [661-1.021] (143) | 8 [6-14] (17) | 107,2 [106,8-108,6] (65,6) | 85,9 [78,2-86,4] (58) |
| rastgeleli | B0 | 63 [50-67] (60) | 0 [0-0] (0) | - | 45,8 [44,7-57,2] (21) |
| rastgeleli | B1 | 1.234 [977-3.578] (51) | 12 [9-21] (0) | 29,5 [29,4-30,8] (28,9) | 92,2 [89,3-92,8] (85) |
| rastgeleli | B2 | 137 [131-307] (79) | 2 [1-12] (0) | 68,7 [66,5-68,8] (63,7) | 75,4 [72,3-78,7] (59) |

`[Şekil: sekil_tls_alarm_tepe_n5000.png]`

**TLS'te yeniden bağlanmanın kendisi canlı alarmı geciktirir.** Bağlantı sonrası iş olmadan bile (B0), üstel kuralda alarm p99 tepesi TLS'siz 47 ms'den 613 ms'ye çıkar (kendi koşularının tabanının 14-22 katı), p99 yaklaşık 8 sn boyunca 100 ms'nin üstünde kalır ve sunucu CPU tepesi %37'den %85'e yükselir; rastgeleli kuralda bu etki yoktur (63 ms). Üstel kural ve B1 birleşiminde TLS ek bir kötüleşme getirmez (8.234 ms; TLS'siz 9.158 ms), çünkü bağlantı sonrası iş sunucuyu zaten doyurmuştur (CPU tepesi %100); boşaltmanın bitişi 43 sn'den 69 sn'ye uzar.

**TLS'siz ortamda sorunsuz olan hücre TLS'te doyar.** Rastgeleli + B1, TLS'siz 51 ms iken TLS'te 1.234 ms'dir (977-3.578; tabanın 30-115 katı) ve sunucu CPU tepesi %92'dir: el sıkışma yükü ile boşaltma yükü aynı çekirdeği paylaşır. B2 (30 sn / 200 ms) bu ortamda işe yarar: her iki kuralda da tepeyi B1'e göre %89 düşürür (üstel 8.234 → 907; rastgeleli 1.234 → 137 ms); bedel, boşaltmanın bitişinin yaklaşık 2,3 kat (rastgeleli: 29,5 → 68,7 sn) ya da 1,5 kat (üstel: 69,2 → 107,2 sn) uzamasıdır. TLS'siz N = 10.000'de aynı B2 ayarı yetmemişti (Bölüm 5.6); burada yeterli olması, B2 ayarının yeterliliğinin toplam yüke bağlı olduğu okumasıyla tutarlıdır, ancak iki yük (TLS + N = 5.000 ile TLS'siz N = 10.000) aynı ölçekte karşılaştırılamaz. Sonuç olarak en iyi politika ortama göre değişir: TLS'siz N = 5.000'de rastgeleli + B1 (51 ms), TLS'te rastgeleli + B2 (137 ms). Tüm 18 koşuda kayıp ve tekrar 0'dır.

*Sınırlar:* TLS'siz karşılaştırma Bölüm 5.5'ten alınmıştır, aynı blokta koşulmamıştır ve izleme süresi farklıdır (tepe ve eşik-üstü ölçütleri sunucu açıldıktan sonraki tüm saniyeler üzerinden alındığından, TLS'siz değerlerin ilk 90 sn'de zaten sönümlendiği görülmüştür). Toplayıcı TLS'sizdir. İstemci tarafı el sıkışma maliyeti ayrılamamıştır. Boşaltmanın uzamasının ve ikinci dalganın mekanizması doğrudan gözlenmemiştir. Rastgeleli/B1'de bir koşu (3,6 sn) diğer ikisinden belirgin ayrılır ve çıkarılmamıştır. Sertifika doğrulama maliyeti ile TLS sürümü ölçülmemiştir.

### 5.9 İkinci sunucu: EMQX (sürümler e1, e2)

Bölüm 5.5'teki düzenek (N = 5.000, TLS'siz, kural × B0/B1/B2, 3 tekrar) ikinci bir sunucuyla, **çok iş parçacıklı EMQX 5.8.6** (Docker imajı imzası sabitlenmiş) ile yinelenmiştir. Sunucu Mosquitto ile aynı kaynak payında (2 CPU / 2 GB; EMQX 2 çalışma iş parçacığı açar) ve ayrıca 4 CPU ile koşulmuştur; kuyruk ve uçuştaki mesaj sınırları Mosquitto ayarına eşdeğerdir (sınırsız kuyruk, uçuşta 100). Bir ölçüm ayarı olarak EMQX'in `force_shutdown` koruması kapatılmıştır: varsayılan ayarda (posta kutusu sınırı 1.000 mesaj) EMQX, boşaltma sırasında tek toplayıcı aboneyi `mailbox_overflow` ile defalarca bağlantıdan koparmış, toplayıcı kopunca mesajlar ve eşitleme yanıtları kaybolmuştur; yani varsayılan ayarla tek yoğun abone boşaltma anında koparılmaktadır. Hipotezler (HE1-HE6) koşulardan önce günlüğe yazılmıştır (`12_adim7_emqx.md`); geçerli bir pilot koşunun sonucu yazım sırasında biliniyordu. Matrisler: E1 (EMQX 2 CPU: kural 2 × B 3 × 3 tekrar = 18 koşu), E2 (EMQX 4 CPU: kural 2 × B1/B2 × 3 tekrar = 12 koşu).

**Tablo 9.** N = 5.000, TLS'siz, canlı alarm gecikmesi tepesi (ms; medyan [en küçük-en büyük]); parantez içinde Mosquitto (Tablo 5). Kaynak: `results/ozet_adim5_e1_hucreler.csv`, `ozet_adim5_e2_hucreler.csv`.

| A | B | EMQX 2 CPU | EMQX 4 CPU | (Mosquitto) |
|---|---|---|---|---|
| üstel | B0 | 112 [105-141] | - | (47) |
| üstel | B1 | 16.188 [12.158-16.266] | 11.527 [11.374-22.805] | (9.158) |
| üstel | B2 | 186 [184-205] | 161 [122-516] | (143) |
| rastgeleli | B0 | 66 [59-112] | - | (60) |
| rastgeleli | B1 | 93 [92-156] | 127 [92-211] | (51) |
| rastgeleli | B2 | 163 [157-309] | 316 [75-6.576] | (79) |

`[Şekil: sekil_emqx_alarm_tepe_n5000.png]`

**"İkinci izdiham" çok çekirdekli sunucuda da vardır.** Aynı kaynak payında EMQX'te üstel kural ve B1'de canlı alarm gecikmesi tepesi 16,2 sn'dir (taban 28-43 ms), Mosquitto'dakinden (9,2 sn) kötüdür; sunucunun iki çekirdeği de dolar (CPU tepesi 207 / 200) ve eşitleme isteklerinin p99'u 9,8 sn'ye çıkar (687-1.649 eşitleme zaman aşımı). Çekirdek sayısını 4'e çıkarmak tepeyi yalnızca %29 düşürür (11,5 sn; sunucu CPU tepesi 389 / 400, yani dört çekirdek de dolar); iki koşuda sorunun tek başına çekirdek sayısıyla çözülmediği görülür. **Aynı CPU payında EMQX her hücrede Mosquitto'dan yüksek tepe verir** (1,1-2,4 kat; örneğin yalnızca bağlanma yükünde, B0 ve üstel kuralda, EMQX 149 / 200, Mosquitto 37 / 100 CPU kullanır): çekirdek başına verim Mosquitto'da daha yüksektir; bunun EMQX'in başka özelliklerinin yükünü içerip içermediği ayrıştırılmamıştır.

**Politika sıralaması iki sunucuda aynıdır.** B1 kilit adım dönüşte (üstel) felakettir; B2 üstelde tepeyi %98-99 düşürür (EMQX: 16.188 → 186 ms); rastgele dönüş B1'i zaten kurtarır ve B2 orada B1'den kötüdür (EMQX: 163 vs 93 ms; Mosquitto: 79 vs 51 ms); B0 tabana yakın kalır (EMQX üstelde 112 ms, Mosquitto'da 47 ms). Dolayısıyla Bölüm 5.5'teki bulgular tek çekirdekli bir sunucuya özgü değildir; büyüklükler sunucuya bağlıdır. Hipotezlerden yalnızca HE1 (üstel + B1'de ikinci izdiham) ve HE2'nin üstel yarısı desteklenmiştir; HE3 (EMQX'te rastgeleli + B1 tabanın ≥ 5 katı; ölçülen yaklaşık 3 kat), HE4 (EMQX'te B0 < 100 ms; üstelde 112 ms) ve HE5 (4 CPU tepeyi ≥ %50 düşürür; %29) çürümüştür.

**Kayıp EMQX'te daha sık, ama küçüktür.** Tekrar 30 koşunun hepsinde 0'dır; kayıp 30 koşunun 17'sinde 1 mesajı aşar (E1'de 14/18, en çok 25; E2'de 3/12, en çok 69; üretilen mesajın en çok %0,011'i), Mosquitto'da 39 koşunun 3'ünde. Nedeni açıklanmamıştır (EMQX'te ham kayıt alınmamıştır).

*Sınırlar:* iki sunucu, tek makine; EMQX tek düğüm ve varsayılandan farklı yapılandırılmıştır (`force_shutdown` kapalı, kuyruk sınırsız), diğer varsayılan ayarlar incelenmemiştir. Aynı CPU payı sunucular arasında adil bir karşılaştırma değildir (çekirdek başına verim farklıdır); 4 CPU matrisi bunun için eklenmiştir, ancak EMQX 4 CPU, simülatör 4 CPU ve toplayıcı 2 CPU, host'un 12 çekirdeğinin 10'unu kullandığından host doymuş olabilir. Mosquitto değerleri aynı blokta koşulmamıştır. E2'de iki aykırı koşu (üstel/B1 tohum 2: 22,8 sn; rastgeleli/B2 tohum 2: 6,6 sn) çıkarılmamış, nedenleri incelenmemiştir. TLS ve N = 10.000 EMQX'te denenmemiştir. EMQX'in kapanışı (2,4 sn) ve açılışı (0,7-0,95 sn) Mosquitto'dan (yaklaşık 0,5 sn) farklıdır; sunucu açılış anının ±2,4 sn kaydırılması sonuçları değiştirmemiştir.

## 6. Tartışma ve Sınırlar

**Bağlanma aşaması deneylerine özgü sınırlar (Bölüm 5.1-5.3).** (i) Kesinti süresi tek (60 sn); üstel kuralın TTR'si kesinti süresine bağlı testere dişi bir fonksiyon olduğundan sonuçlar bu süreye özgüdür ve kesinti süresi taraması yapılmamıştır. (ii) Yalnızca iki cihaz sayısı (2.000, 5.000) denenmiştir. (iii) Sunucu açıkken görülen başarısız bağlantıların ve sabit kuraldaki faz etkisinin mekanizması doğrudan incelenmemiştir. (iv) Simülatörün CPU tepesi sınırın yaklaşık %61'ine çıkmıştır. (v) Koşular sırasında Docker'da başka bir projeye ait iki boşta konteyner çalışmaktaydı; etkisi ölçülmemiştir.

**Bağlantı sonrası yük deneylerine özgü sınırlar.** (i) Mosquitto tek iş parçacıklıdır; bulgular çok çekirdekli bir sunucuya genellenemez. (ii) Toplayıcı tek istemcidir ve onun yavaşlaması gecikmeye karışmış olabilir; toplayıcının işlem yükü ölçülmemiştir. (iii) N = 10.000'de taban gecikmenin yükselmesi simülatörün yükünü düşündürür; doğrulanmamıştır. (iv) 39 koşunun 6'sında 1 ile 132 arasında mesaj, toplayıcıya ulaşmamıştır (toplam yaklaşık 540 bin ile 1,6 milyon mesaj içinde, yani en çok yaklaşık %0,01). Ham kayıt açıkken yapılan 12 ek koşuda (iki hücre, 6'şar tekrar) kayıp görülmemiş, olay yeniden üretilememiştir; nedeni açıklanamamıştır. Olası ama doğrulanmamış açıklamalardan biri, sunucunun açıldığı an ile toplayıcının yeniden abone olduğu an arasındaki yaklaşık 0,4-0,6 sn'lik pencerede cihazların gönderdiği mesajların abonesiz kalmasıdır (ölçülen yalnızca pencerenin varlığıdır); diğeri, durdurma anında sunucu belleğindeki iletilmemiş mesajların kaybıdır. Kaybın ham kayıt tarafından bastırılmış olması da dışlanamamıştır (günlük `09`). (v) B2 yalnızca iki ek ayarla sınanmış, ayar uzayı taranmamıştır. (vi) Biriken vital ve alarm mesajları aynı sırayla boşaltılır; alarmları önceliklendirmek gelecek çalışmadır. (vii) İş yükü (mesaj boyutu, vital ve alarm sıklığı, eşitleme boyutu) varsayımsaldır, klinik gerçekliği temsil etmez.

`[ ]` Sınırlar (günlüklerden toplanacak): tek bilgisayar, sanal cihaz, sıfır gecikmeli ağ, TLS yalnızca bağlanma aşamasında denenmiş (doğrulamasız), yalnızca iki sunucu (Mosquitto ve EMQX; ikisi tek düğüm), tek kesinti türü (temiz), tekrar sayısı, CPU örnekleme sıklığı, çözünürlük 1 sn, mekanizmaların bir kısmı doğrudan gözlenmedi.

## 7. Sonuç ve Gelecek Çalışma

`[ ]` Sonuç (Adım 5 için ön metin, Bölüm 5.1-5.7 birleştirilecek): bu ortamda, aynı anda dönen çok sayıda cihazın bağlantı sonrası işi (eşitleme ve birikenin boşaltılması) canlı alarm gecikmesini bağlanmanın kendisinden çok daha fazla artırmıştır; dönüşü dağıtmak bu etkiyi N = 5.000'de ortadan kaldırmış, N = 10.000'de yetmemiştir; boşaltmayı zamana yayan B2 politikası ancak parametreleri cihaz sayısına göre büyütüldüğünde N = 10.000'de gecikmeyi yüzlerce milisaniyeye indirmiş, bedel olarak boşaltmanın yaklaşık 4 kat uzamasına yol açmıştır. Bağlanma aşamasında ise TLS'siz ortamda sunucu zorlanmamış ve rastgeleli kuralın yalnızca bedeli ölçülmüş, TLS ile sunucu CPU'su 3,5-5,2 kat yükselmiş ve rastgeleli kural toparlanmayı yarıdan fazla kısaltıp sunucu açıkken başarısız bağlantıyı sıfırlamıştır (Bölüm 5.4). TLS ile bağlantı sonrası iş birlikte ölçüldüğünde TLS'siz ortamda en iyi olan politika (rastgeleli + B1) doymuş, B2 yeniden en iyi politika olmuştur (Bölüm 5.8): en iyi politika ortama göre değişir. İkinci bir sunucu olan çok çekirdekli EMQX'te de ikinci izdiham görülmüş, politika sıralaması aynı kalmış, çekirdek sayısını 4'e çıkarmak üstel + B1'deki gecikmeyi yalnızca %29 azaltmıştır (Bölüm 5.9). Gelecek çalışma adayları: sessiz kesinti, TLS'te oturum devam ettirme ve toplayıcının da TLS'li olması, kenar geçidi katmanı, sunucunun "ne kadar sonra gel" bilgisi vermesi, alan adı çözümlemesinin toplu yeniden bağlanmadaki rolü.

## Kaynakça

Bkz. `kaynakca.md`. Metinde geçenler:

[1] I. Fette ve A. Melnikov, "The WebSocket Protocol," RFC 6455, IETF, Aralık 2011. `[künye kontrol]`

[2] C. Jennings, R. Mahy ve F. Audet, "Managing Client-Initiated Connections in the Session Initiation Protocol (SIP)," RFC 5626, IETF, Ekim 2009. `[künye kontrol]`

## Ekler

- EK-A: Deney kayıt defteri (`results/deney_kayitlari.csv`)
- EK-B: Koşu adları ve girdi dosyaları
- EK-C: Kod deposu bağlantısı (github.com/mertbayir/mqtt-analysis-project)
