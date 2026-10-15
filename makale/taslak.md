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

Bu çalışma yeni bir algoritma önermez. Bilinen bekleme kurallarını, açık kaynaklı bir MQTT 5 sunucusu ve kendi geliştirdiğimiz bir cihaz simülatöründen oluşan tekrarlanabilir bir ortamda karşılaştırır. Katkılarımız: (i) tekrarlanabilir bir ölçüm ortamı ve kayıt düzeni, (ii) üç bekleme kuralının toplu yeniden bağlanma altında karşılaştırması, (iii) ölçüm ortamının kendisinin sonuçları nasıl çarpıtabildiğine dair doğrulanmış bulgular (Bölüm 4). `[ ]` bağlantı sonrası yükün ölçümü yapılırsa (iv) eklenecek.

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

Ortam iki katmandan oluşur: uç cihazları temsil eden bir simülatör ve merkezi bir MQTT 5 sunucusu. İkisi de aynı bilgisayarda, ayrı Docker konteynerlerinde, aynı Docker ağı içinde çalışır. Sunucu Eclipse Mosquitto 2.1.2'dir, 2 CPU ve 2 GB bellekle sınırlanmıştır; simülatör 4 CPU ve 3 GB ile çalışır. TLS ve kimlik doğrulama kullanılmamıştır. Aradaki bir kenar geçidi (edge gateway) katmanı kapsam dışıdır.

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

### 3.3 Kesinti senaryosu

*Durum: yazılabilir.*

Cihazlar 20 sn içinde yayılarak bağlanır (başlangıç fırtınası olmasın diye). Başlangıçtan 40 sn sonra sunucu konteyneri durdurulur (temiz kesinti: bağlantılar sonlanır, cihazlar kopmayı hemen öğrenir), 60 sn sonra yeniden başlatılır ve 120 sn daha izlenir. Sessiz kesinti (ağın sessizce kopması) kapsam dışıdır.

### 3.4 Ölçütler

*Durum: tanımlar `scripts/ozet.ps1` başında.*

Toparlanma süresi (TTR): sunucunun başlatıldığı andan bağlı cihazların %50, %95 ve %100'üne ulaşılana kadar geçen süre (çözünürlük 1 sn). Kesintideki ve sonrasındaki tepe bağlantı denemesi/sn. Sunucu açıkken başarısız bağlantı sayısı. Sunucu ve simülatör CPU'su (~0,7 Hz örnekleme). `[ ]` Alarm gecikmesi ve biriken mesaj boşaltma ölçütleri (yapılırsa).

### 3.5 Deney ortamı

*Durum: `00_ortam.md`'den tablo taşınacak.* `[ ]`

### 3.6 Kayıt ve tekrarlanabilirlik

*Durum: yazılabilir.* Her koşu bir girdi dosyası (`girdi.json`), saniyelik ham çıktı (CSV), kesinti zamanları ve kod sürümü (git commit) ile kayıt defterine işlenir. Özet sayılar elle değil betikle üretilir. Hatalı koşular silinmez, geçersiz olarak işaretlenir.

## 4. Ölçüm Ortamının Doğrulanması

*Durum: malzeme hazır (günlük `04`, Bulgu 1 ve 5). Bu bölüm çalışmanın güvenilirlik göstergesi.*

- **4.1 Zaman aşımı hatası.** `[ ]` MQTTnet `WithTimeout`, yanıt vermeyen sunucuya TCP bağlantı kurulumunu sınırlamıyordu; cihazlar bekleme kuralı hiç devreye girmeden tek bir asılı denemede kaldı. Kesinti sırasındaki deneme sayısı incelenince fark edildi. (koşular: `adim3_*_v1`)
- **4.2 Ad çözümlemesi artefaktı.** `[ ]` Cihazlar sunucuyu adıyla çağırdığında 5.000 cihazın toparlanması 27-69 sn sürdü, IP ile 2,1 sn. Sunucu MAC adresi ve çekirdek kuyruk taşması elenmiştir. (koşular: `v2`, `v2b`, `v3mac`, `v4ip`). Mekanizma doğrudan gözlenmedi.
- **4.3 Çıkarım.** `[ ]` Toplam "bağlandı" sayısı yetmez, kesinti sırasındaki deneme sayısı da doğrulanmalı; ölçüm ortamının kendisi "fırtına"yı üretebilir ya da gizleyebilir.

## 5. Bulgular

*Durum: BOŞ. Tekrarlı koşular (IP ile, 3 kural, N = 2.000 ve 5.000) gerekiyor.*

- `[ ]` 5.1 Üç bekleme kuralının deneme dağılımı (Şekil)
- `[ ]` 5.2 Cihaz sayısına göre toparlanma süresi (Şekil, medyan ve aralık)
- `[ ]` 5.3 Sunucu yükü
- `[ ]` 5.4 TLS'li durum (pilot sonrası)
- `[ ]` 5.5 Bağlantı sonrası yük, alarm gecikmesi (yapılırsa)

*Ön gözlem (yalnızca 100 cihaz, tek koşu, günlük `03`):* rastgeleli üstel kural tepe deneme sayısını 100'den 39'a düşürdü, toparlanmayı 2,5 sn'den 16 sn'ye uzattı. Bu, [1] ve [2]'deki bilinen eğilimle uyumlu. **Nihai bulgu olarak kullanılmayacak.**

## 6. Tartışma ve Sınırlar

`[ ]` Sınırlar (günlüklerden toplanacak): tek bilgisayar, sanal cihaz, sıfır gecikmeli ağ, TLS'siz, tek sunucu, tek kesinti türü (temiz), tekrar sayısı, CPU örnekleme sıklığı, çözünürlük 1 sn, mekanizmaların bir kısmı doğrudan gözlenmedi.

## 7. Sonuç ve Gelecek Çalışma

`[ ]` Sonuç sonuçlar belli olunca. Gelecek çalışma adayları: sessiz kesinti, TLS maliyeti, kenar geçidi katmanı, sunucunun "ne kadar sonra gel" bilgisi vermesi, alan adı çözümlemesinin toplu yeniden bağlanmadaki rolü.

## Kaynakça

Bkz. `kaynakca.md`. Metinde geçenler:

[1] I. Fette ve A. Melnikov, "The WebSocket Protocol," RFC 6455, IETF, Aralık 2011. `[künye kontrol]`

[2] C. Jennings, R. Mahy ve F. Audet, "Managing Client-Initiated Connections in the Session Initiation Protocol (SIP)," RFC 5626, IETF, Ekim 2009. `[künye kontrol]`

## Ekler

- EK-A: Deney kayıt defteri (`results/deney_kayitlari.csv`)
- EK-B: Koşu adları ve girdi dosyaları
- EK-C: Kod deposu bağlantısı (github.com/mertbayir/mqtt-analysis-project)
