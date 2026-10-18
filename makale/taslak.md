# Hasta İzleme Gibi Çok Cihazlı MQTT 5 Sistemlerinde Toplu Yeniden Bağlanma ve Bağlantı Sonrası Yük: İki Sunucuda Bir Ölçüm Çalışması

*(Çalışma başlığı; son sürümde netleştirilecek.)*

**Yazar:** Mert Bayır  
**Ders:** Bilimsel Araştırma, Proje Hazırlama ve Seminer, Sakarya Üniversitesi  
**Durum:** TASLAK. Tüm deneyler (Bölüm 5.1-5.12) tamamlandı. İlgili Çalışmalar yazıldı (18 Ekim); kalan `[ ]` işaretleri taranacak.

---

## Özet

Çok sayıda cihazın tek bir MQTT 5 sunucusuna bağlandığı sistemlerde, sunucu kesintisinden sonra cihazların aynı anda yeniden bağlanması ve bağlandıktan sonra yaptıkları işler (durum eşitleme, kesintide biriken mesajların gönderilmesi) canlı mesajların gecikmesini artırabilir. Bu çalışma, üç yeniden bağlanma bekleme kuralını (sabit, üstel, rastgeleli üstel) ve dört bağlantı sonrası politikayı (biriken atılır, hemen boşalt, zamana yay, alarmı öne al), iki açık kaynaklı sunucuda (Mosquitto 2.1.2, EMQX 5.8.6), 10.000 cihaza kadar simüle edilen bir ortamda, TLS'li ve TLS'siz, tekrarlı ve kayıtlı koşularla karşılaştırır; canlı alarm gecikmesi, birikmiş alarmın yaşı ve sunucu yükü ölçülür. Bulgular: bağlantı sonrası iş, canlı alarm gecikmesini 5.000 cihazda yaklaşık 30 ms'den yaklaşık 9 sn'ye çıkarabilir (üstel kural, Mosquitto, TLS'siz); dönüşü rastgele dağıtmak sunucu zorlandığında (TLS ya da çok sayıda cihaz) belirgin fayda sağlar, rahat ortamda yalnızca bedel getirir; boşaltmayı zamana yaymak uygun ayarla gecikmeyi %89-99 düşürür, ama ayarı yüke bağlıdır ve birikmiş veriyi geciktirir; alarmı öne almak, denenen üç hücrede birikmiş alarmın yaş medyanını %24-60 azaltır; sıralama çok çekirdekli EMQX'te de geçerlidir ve çekirdek eklemek tek başına yetmez. Sonuçlar tek makine, sanal cihaz ve temiz kesinti ile sınırlıdır.

## Abstract

In systems where many devices connect to a single MQTT 5 broker, a broker outage makes devices reconnect at the same time, and the work they do after reconnecting (state synchronisation and sending the messages buffered during the outage) can delay live messages. This study compares three reconnection back-off rules (fixed, exponential, randomised exponential) and four post-connect policies (drop the backlog, flush at once, spread the flush, send alarms first) on two open-source brokers (Mosquitto 2.1.2 and EMQX 5.8.6), with up to 10,000 simulated devices, with and without TLS, using repeated and fully recorded runs. Live alarm latency, the age of buffered alarms and broker load are measured. Post-connect work can raise live alarm latency from about 30 ms to about 9 s with 5,000 devices (exponential rule, Mosquitto, no TLS). Randomising the reconnection helps clearly when the broker is under stress (TLS or many devices) and only costs time when it is not. Spreading the flush cuts the latency by 89-99% with a suitable setting, but the setting depends on the load and delays the buffered data. Sending alarms first reduces the median age of buffered alarms by 24-60% in the three cells tested. The ranking of policies holds on the multi-core EMQX broker, and adding cores alone is not enough. The results are limited to a single machine, simulated devices and a clean outage.

**Anahtar kelimeler:** MQTT 5, toplu yeniden bağlanma, üstel geri çekilme, bağlantı sonrası yük, nesnelerin interneti, hasta izleme

---

## 1. Giriş

Çok sayıda cihazın tek bir merkezi sunucuya kalıcı bağlantıyla bağlandığı sistemlerde, sunucunun ya da ağın kısa süreli kesintisi cihazların hemen hemen aynı anda yeniden bağlanmaya çalışmasına yol açar. Hasta başı monitörleri ve giyilebilir sensörler gibi tıbbi nesnelerin interneti (IoMT) dağıtımlarında bu durum önem taşır: kesinti sırasında biriken ölçümler ve sunucudan alınması gereken güncel yapılandırma, cihazlar geri döndüğü anda sunucuya yeni bir yük bindirir ve bu yük, toparlanma sürerken akmaya devam eden canlı mesajları, örneğin alarmları, geciktirebilir.

Yeniden bağlanma anındaki bağlantı yükünü azaltmak için bilinen yöntem, cihazların bağlanmadan önce rastgele bir süre beklemesi ve başarısız denemelerde bekleme süresini üstel olarak artırmasıdır [1], [2]. Ancak cihaz bağlandıktan sonra da yük sürer: cihaz kesinti sırasında kaçırdığı durumu eşitler ve biriktirdiği mesajları gönderir. Bu *bağlantı sonrası yükün* bağlanma aşamasından ayrı ölçüldüğü, MQTT 5 üzerinde canlı trafik sürerken yapılmış açık bir çalışma, bildiğimiz kadarıyla bulunamamıştır. (arama kapsamı ve sınırı Bölüm 2.6'da). Bu soruya en yakın çalışmalar, tek istemcili geri yükleme [7], hücresel çekirdekte sunucu tarafı hız sınırı [6] ve tek geçitli öncelikli geri doldurma simülasyonudur [8]; farkımız Bölüm 2'de açıklanmıştır.

Bu çalışma yeni bir algoritma önermez. Bilinen bekleme kurallarını ve bağlantı sonrası boşaltma politikalarını, iki açık kaynaklı MQTT 5 sunucusu (tek iş parçacıklı Mosquitto ve çok iş parçacıklı EMQX) ve kendi geliştirdiğimiz bir cihaz simülatöründen oluşan tekrarlanabilir bir ortamda karşılaştırır. Dört araştırma sorusu sorulur:

- **S1.** Bağlanma aşamasında üç bekleme kuralı (sabit, üstel, rastgeleli üstel) toplu yeniden bağlanma altında nasıl davranır ve sunucu ne zaman zorlanır? (Bölüm 5.1-5.4)
- **S2.** Cihazlar bağlandıktan sonra yaptıkları iş (durum eşitleme ve kesintide biriken mesajların boşaltılması), canlı alarm gecikmesini bağlanma aşamasından ayrı olarak ne kadar etkiler? (Bölüm 5.5-5.6, 5.8)
- **S3.** Bu işi zamana yaymak (B2) işe yarar mı, ayarı yüke nasıl bağlıdır ve bedeli nedir? (Bölüm 5.7)
- **S4.** Bulgular sunucuya (tek çekirdekli Mosquitto, çok çekirdekli EMQX) ve çekirdek sayısına özgü müdür? (Bölüm 5.9)

Katkılarımız: (i) tekrarlanabilir bir ölçüm ortamı ve kayıt düzeni; (ii) üç bekleme kuralının toplu yeniden bağlanma altında karşılaştırması, TLS'siz ve TLS'li; (iii) ölçüm ortamının kendisinin sonuçları nasıl çarpıtabildiğine dair doğrulanmış bulgular (Bölüm 4); (iv) bağlantı sonrası yükün canlı alarm gecikmesine etkisinin bağlanma aşamasından ayrı ölçülmesi ve üç boşaltma politikasının karşılaştırılması; (v) politika sıralamasının ortama (TLS, cihaz sayısı) bağlı olduğunun ve çekirdek eklemenin tek başına çözüm olmadığının gösterilmesi; (vi) bedelin, yani kesintide biriken alarmın sunucuya ulaşma yaşının da ölçülmesi ve bu bedeli azaltmaya yönelik küçük bir politikanın (B3, alarm öncelikli boşaltma) değerlendirilmesi (Bölüm 5.10). Bu bir ölçüm çalışmasıdır; yeni bir algoritma önerisi iddiası taşımaz. B3'ün dayandığı fikir, yani öncelikli ve hız sınırlı geri doldurma, sera IoT'si için tek bir uç geçitte simülasyonla değerlendirilmiştir [8]; biz bu fikirleri çok cihazlı, gerçek MQTT sunucularında ve canlı alarm gecikmesi ölçütüyle değerlendirir, hangi koşullarda fayda ve bedel getirdiğini raporlarız.

Çalışma tek bir bilgisayarda ve sanal cihazlarla yapılmıştır; deneylerin çoğu TLS'sizdir, TLS yalnızca Bölüm 5.4 ve 5.8'de denenmiştir; sonuçlar bu ortama göreceli olarak raporlanır, mutlak kapasite iddiası taşımaz. Tıbbi çerçeve bir senaryo olarak kullanılmıştır, klinik güvenlik iddiası yoktur.

## 2. İlgili Çalışmalar

Bu bölüm, çalışmamızın dayandığı ve ona en yakın yayınları, her birinin ne yaptığını ve bizden farkını özetler. Bölüm 2.6 arama kapsamını ve sınırını belirtir.

### 2.1 Yeniden bağlanma kuralları ve eşzamanlılık

WebSocket protokolü, kopma sonrası çok sayıda istemcinin hemen ve ısrarla yeniden bağlanmasının sunucuda bir hizmet engelleme saldırısına benzer bir etki yaratabileceğini belirtir; ilk yeniden bağlanmanın rastgele geciktirilmesini (0-5 sn arası bir değer makul bir ilk gecikmedir) ve sonraki denemelerin kesilmiş ikili üstel geri çekilme gibi bir yöntemle uzatılmasını önerir [1, §7.2.3]. SIP için RFC 5626 bekleme üst sınırını `W = min(en çok, taban × 2^ardışık hata)` olarak tanımlar, gerçek beklemeyi W'nin %50-%100'ü arasında düzgün rastgele seçer ve varsayılan olarak taban 30 sn (ya da 90 sn) ile tavan 1800 sn önerir [2, §4.5]. SIP'te sunucu, 503 yanıtındaki `Retry-After` başlığıyla istemcinin beklemesini uzatabilir [2, §4.5]; MQTT 5'te bunun sayısal karşılığı yoktur (Bölüm 2.4). Bu çalışmanın bekleme kuralları bu iki standarttan türetilmiştir (Bölüm 3.2).

Rastgele gecikmenin kuramsal dayanağı Floyd ve Jacobson'a aittir: bağımsız görünen çok sayıda işlem istemeden eşzamanlı hâle gelebilir, eşzamanlı olmayandan eşzamanlıya geçiş yavaş değil ani bir faz geçişidir ve eşzamanlılığı önlemek için eklenmesi gereken rastgelelik şaşırtıcı derecede büyüktür; yazarlar basit bir kural olarak zamanlayıcıyı her turda ortalama periyodun %50-%150'si aralığında düzgün rastgele kurmayı önerir [3, §5.3, §7]. Aynı çalışma, bir sunucu arızadan ya da yoğunluktan toparlanırken istemcilerin kurtarma işlemlerinde eşzamanlı hâle gelmesini ve toparlanmanın bu yüzden gecikmesini bir örnek olarak anar [3, §1]. Çalışma sürekli periyodik mesajlara (yönlendirme güncellemeleri) odaklanır; bizim durumumuz ise kesintinin tüm cihazları aynı anda başlattığı tek seferlik bir eşzamanlamadır.

### 2.2 Eşzamanlı yeniden bağlanma, yeniden deneme ve aşırı yük

Huang ve ark., kamuya açık olay raporlarından metastabil çöküş örnekleri (tetikleyici kalktıktan sonra bile sürüp giden aşırı yük) derlemiş ve en yaygın sürdürücü etkinin yeniden deneme politikası olduğunu bildirmiştir (raporlanan 21 olayın 11'i) [4, §2]. Aynı çalışma, tetikleyicinin süresi uzadıkça sistemin daha kırılgan hâle geldiğini kuramsal olarak savunur ve bir deneyde tetikleyiciyi 10 sn yerine 9 sn tutmanın çöküşü önlediğini gösterir [4, §3.5, §5.2]. Isaacs ve ark., metastabil çöküşe duyarlı bölgeyi önceden bulmak için kuyruk modeli, ayrık olay simülasyonu, emülatör ve gerçek sistemde stres testinden oluşan bir araç zinciri önerir; soyut modellerin nicel tahminlerinin sapabileceğini ve gerçek sistemde doğrulamanın en inandırıcı adım olduğunu vurgular [5, §3, §6]. İki çalışma da istek/cevap servislerini ele alır; yeniden bağlanma, kalıcı bağlantı ve MQTT bu çalışmaların kapsamı dışındadır.

Eşzamanlı yeniden bağlanmanın ardışık aşırı yük yarattığı, hücresel çekirdek ağlarında da raporlanmıştır. Xie ve ark. (Spillway), bölgesel bir arızanın milyonlarca cihazı aynı anda koparması ve cihazların yaklaşık aynı anda yeniden bağlanmaya çalışması hâlinde iç kuyrukların dolduğunu, isteklerin cihazın zaman aşımından sonra işlenip boşa çalışıldığını ve yeniden denemelerin bir geri besleme döngüsü kurduğunu anlatır [6, §2.3]. Çözümleri sunucu tarafındadır: standart hız sınırı parametrelerini (jeton kovası, oturum hız sınırı, kuyruk derinliği gibi) bir sayısal ikiz ve Bayes optimizasyonuyla ayarlayıp önceden kurmak; beş yıllık bir dağıtım ve geçmiş olaylara karşı doğrulama rapor edilir [6, §7-8]. Optimize edilen kritik parametreler ağ elemanlarının hız sınırlarıdır; cihaz tarafı geri çekilme taban süresi, jitter aralığı ve deneme sayısı kritik sayılmaz [6, Tablo 7]. Çalışma MQTT veya WebSocket gibi uygulama katmanı kalıcı bağlantıları, bağlantı sonrası birikmiş veriyi ya da mesaj gecikmesini ele almaz. Bizim çalışmamız bu üç noktada farklıdır.

### 2.3 Kopma sonrası biriken verinin geri yüklenmesi

Domingues ve ark., kopma sırasında yerel veritabanında biriken sensör verisinin MQTT ile sunucuya geri yüklenmesini incelemiştir: bağlantı döner dönmez sunucu özel bir konuya yayın yaparak geri yüklemeyi başlatır, istemci veriyi parçalar hâlinde gönderir ve bitiş mesajı yollar [7, §3]. Asıl soru tek bir mesajın en uygun yük boyutudur; dijital sağlık senaryosunda (hasta yatağında akıllı kutu) tek bir istemciyle yapılan 4.300 testte yaklaşık 13 MB'lık yük en kısa geri yükleme süresini vermiş ve bu değer kesinti süresine bağlı çıkmamıştır [7, §5]. Çok küçük parçaların tek istemcide bile sunucuda kuyruk yarattığı (saniyede yaklaşık 300 mesaj) bildirilmiştir [7, §5]. Geri yükleme sürerken canlı veri de gönderildiğinde geri yükleme süresinin neredeyse değişmediği görülmüş, ancak canlı mesajın gecikmesi bir ölçüt olarak raporlanmamıştır [7, §5]. Yazarlar, aynı anda geri yükleme yapan istemci sayısının etkisinin incelenmesini gelecek çalışmaya bırakır [7, §6]. Bizim çalışmamız bu soruyu 10.000 cihaza kadar ve canlı alarm gecikmesi ölçütüyle ele alır.

Bi ve ark., sera IoT'sinde uç geçit ile bulut arasındaki kopmalar için bir mekanizma önerir; bağlantı dönünce biriken her şeyi hemen yüklemenin patlamaya ve yüksek öncelikli olayların gecikmesine yol açtığını, bu nedenle olay-öncelikli ve kademeli hız sınırlı bir geri doldurma kullandıklarını anlatır [8, §3.3]. Tek bir geçitle yapılan simülasyonda hız sınırı zirve verimi düşürmüş, öncelik ise yüksek öncelikli olayların bitişini belirgin biçimde öne çekmiştir [8, §5.1, §5.6]. Bu fikirler, B3'ün (Bölüm 3.2) dayandığı fikre yakındır. Çalışma bir MQTT sunucusu, çok sayıda cihaz, bekleme kuralı ve canlı mesaj gecikmesi içermez; yazarlar sonuçların bir saha platformunda doğrulanmadığını belirtir [8, §5.8].

### 2.4 MQTT 5 standardı ve sunucunun ipuçları

MQTT 5.0 standardı, sunucunun yoğunluğunu `Server busy` ve `Connection rate exceeded` gibi neden kodlarıyla bildirmesine, `Server Reference` ile istemciyi başka bir sunucuya yönlendirmesine ve `Server Keep Alive` ile istemcinin canlı tutma süresini değiştirmesine izin verir [9, §3.2.2.2, §3.2.2.3.14, §3.2.2.3.16]. Oturum (`Session Expiry Interval`) ve ileti ömrü (`Message Expiry Interval`) için de özellikler tanımlar [9, §3.1.2.11.2, §3.3.2.3.3]. Standardın özellik tablosunda ve metin taramamızda, istemciye ne kadar bekleyeceğini bildiren bir özellik ya da geri çekilme ve rastgelelik önerisi bulunamadı; yeniden bağlanma zamanlaması istemci politikasına bırakılmıştır. Karşılaştırma için: SIP'te sunucu `Retry-After` ile bekleme süresi bildirebilir [2, §4.5]; Spillway'in yazarları da 3GPP'de uçtan uca geri basınç protokolünün bulunmamasını bir ders olarak belirtir [6, §9].

### 2.5 Tıbbi çerçeve

Tıbbi nesnelerin interneti çerçevesi için bu taslakta dayanağımız Domingues ve ark.'nın dijital sağlık senaryosudur [7]. Başka tıbbi IoT çalışmaları bu taslakta okunmadığı için burada özetlenmemiştir.

### 2.6 Konumumuz ve arama kapsamı

| Çalışma | Katman ve sistem | Cihaz sayısı | Ele alınan | Ölçüt | Bizden farkı |
|---|---|---|---|---|---|
| [1], [2], [3] | Standartlar ve kuram | Yok / simülasyon | Bekleme kuralı, rastgelelik | Kuramsal / simülasyon | Kuralları gerçek sunucuda ölçmüyorlar |
| [4], [5] | İstek/cevap servisleri | Tek/birkaç istemci | Yeniden deneme, metastabilite | Gecikme, çöküş | Kalıcı bağlantı, yeniden bağlanma ve MQTT yok |
| [6] | 5G çekirdek sinyalleşmesi | Milyonlar (model ve gerçek olay) | Sunucu tarafı hız sınırı | Geri düşen kullanıcı, toparlanma süresi | Sunucu tarafı; MQTT, birikmiş veri ve canlı gecikme yok |
| [7] | MQTT (Mosquitto) | 1 | Birikmiş veriyi geri yükleme, yük boyutu | Geri yükleme süresi, verim | Tek istemci; canlı gecikme ölçüt değil |
| [8] | Sera IoT, uç geçit | 1 geçit | Öncelikli, hız sınırlı geri doldurma | Zirve verim, tamamlanma süresi | Simülasyon; MQTT sunucusu, çok cihaz, canlı gecikme yok |
| **Bu çalışma** | MQTT 5 (Mosquitto, EMQX) | 500-10.000 | Bekleme kuralı ve bağlantı sonrası boşaltma politikası | **Canlı alarm gecikmesi**, birikmiş alarmın yaşı | |

Özetle: yeniden bağlanma kuralları standartlarla önerilmiş [1], [2] ve eşzamanlılığın kuramı kurulmuş [3]; yeniden denemenin aşırı yükü sürdürdüğü gösterilmiş [4], [5] ve hücresel çekirdekte sunucu tarafında azaltılmış [6]; biriken verinin geri yüklenmesi tek istemci için incelenmiş [7] ve öncelikli, hız sınırlı geri doldurma tek bir geçit için simüle edilmiştir [8]. Bildiğimiz kadarıyla bu parçaları, yani çok cihazlı bir MQTT 5 sisteminde cihaz tarafı bekleme kuralını ve bağlantı sonrası boşaltma politikasını canlı mesaj gecikmesi ölçütüyle gerçek sunucularda birlikte inceleyen açık bir ölçüm çalışması bulunamamıştır.

**Arama kapsamı ve sınırı.** Bu cümle sistematik bir literatür taramasına dayanmaz ve yokluğun kanıtı değildir. Arama Ekim 2026'da genel web araması ve açık erişimli yayıncı sayfaları üzerinden yapılmış, atıf yapılan çalışmalar tam metinden (kısmen tam metinden) okunmuştur; Google Scholar ve ücretli veritabanları (IEEE Xplore, Scopus, Web of Science) taranmamıştır. Yalnızca özet düzeyinde görülen çalışmalar (örneğin mesaj önceliği üzerine MQTT çalışmaları) atıf listesine alınmamıştır.

## 3. Yöntem

### 3.1 Sistem mimarisi

Ortam üç bileşenden oluşur: uç cihazları temsil eden bir simülatör, merkezi bir MQTT 5 sunucusu ve (Bölüm 5.5 ve sonrasında) gecikmeyi ölçen bir toplayıcı. Hepsi aynı bilgisayarda, ayrı Docker konteynerlerinde, aynı Docker ağı içinde çalışır. **Sunucular:** Eclipse Mosquitto 2.1.2 (tek iş parçacıklı) ve EMQX 5.8.6 (çok iş parçacıklı, Erlang); her ikisi de 2 CPU ve 2 GB bellekle sınırlanmıştır (EMQX ayrıca 4 CPU ile denenmiştir), aynı sabit MAC adresi ve aynı IP adresiyle; anonim erişim. **Simülatör:** 4 CPU ve 3 GB. **Toplayıcı:** tüm cihaz mesajlarına abone olur, gecikmeyi ölçer ve cihazların durum eşitleme isteklerine yanıt verir (2 CPU, 1 GB). TLS yalnızca Bölüm 5.4 ve 5.8'de açıktır (cihazlar 8883 numaralı portta TLS ile bağlanır; toplayıcı TLS'siz kalır), diğer deneyler TLS'sizdir. Kimlik doğrulama kullanılmamıştır. Aradaki bir kenar geçidi (edge gateway) katmanı kapsam dışıdır.

`[Şekil 1: sekil_mimari.png (a: bileşenler, b: deney zaman çizelgesi)]`

### 3.2 Sanal cihaz ve yeniden bağlanma kuralları

Her sanal cihaz, MQTTnet 5.2 ile MQTT 5 üzerinden sunucuya bağlanan bir asenkron görevdir (kalp atışı 10 sn, bağlantı zaman aşımı 5 sn, kütüphanenin otomatik yeniden bağlanması kapalı). Bağlantı koptuğunda cihaz bir bekleme kuralına göre bekler ve yeniden dener. Her cihazın rastgele sayı üreteci, deney tekrarlanabilsin diye cihaz numarası ve deney tohumundan türetilir. Üç kural karşılaştırılmıştır:

| Kural | Tanım |
|---|---|
| Sabit | Her seferinde 1 sn |
| Üstel | `min(30, 1 × 2^(n−1))` sn, rastgelelik yok |
| Rastgeleli üstel | 1. bekleme U(0, 5) sn [1]; sonrakiler `W = min(30, 1 × 2^(n−1))` olmak üzere U(0,5·W, W) [2] |

Rastgeleli üstel kural, [1] ve [2]'nin birleşimidir; iki standardın birebir uygulaması değildir. Özellikle [2]'nin varsayılan sabitleri (taban 30 sn ya da 90 sn, tavan 1800 sn) kullanılmamıştır: bekleme, kesinti ve ölçüm süresine uygun olacak biçimde taban 1 sn ve tavan 30 sn alınmıştır. [2]'nin varsayılanlarıyla ilk yeniden deneme ilk başarısızlıktan 30-60 sn sonra olurdu ve tavana ulaşınca denemeler arası 15-30 dakika olurdu [2, §4.5].

**İş yükü ve bağlantı sonrası politikalar (Bölüm 5.5 ve sonrası).** Her cihaz 2 sn'de bir 128 baytlık bir vital bulgu mesajı ve 10 sn'de bir 128 baytlık bir alarm mesajı üretir (QoS 1; mesajın planlanan gönderim anı önceden belirlidir). Bağlantı yokken üretilen mesajlar cihazda bellek içi kuyrukta birikir. Cihaz **yeniden bağlandığında** (ilk açılışta değil) bir *bağlantı sonrası politika* uygular: sunucudan 4.096 baytlık güncel durumu ister (eşitleme, MQTT istek/yanıt) ve birikenleri gönderir. Dört politika karşılaştırılmıştır (B3, Bölüm 5.10'da):

| Politika | Tanım |
|---|---|
| B0 (referans) | Biriken atılır, eşitleme yok. Yalnızca referans olarak kullanılır; veriyi attığı için pratik bir politika değildir |
| B1 (hemen) | Bağlanır bağlanmaz eşitleme, ardından birikenin tamamı peş peşe |
| B2 (ayrık) | Bağlandıktan sonra U(0, W) sn bekle, eşitle, birikeni *a* ms aralıkla gönder. W = 30 sn ve a = 200 ms (varsayılan); Bölüm 5.7'de (W, a) = (60 sn, 1 sn) ve (90 sn, 500 ms) de denenmiştir |
| B3 (alarm öncelikli) | Bağlanır bağlanmaz, eşitlemeden önce, kesintide biriken **alarmlar** peş peşe gönderilir (cihaz başına yaklaşık 6-7 mesaj); kalan (vital) mesajlar B2 gibi (U(0, W) gecikme, eşitleme, *a* ms aralık) boşaltılır |

Cihazın bağlandıktan sonra sunucudan durum istemesi ve biriken veriyi göndermesi, [7]'deki istek-yanıt temelli geri yükleme akışına tasarım olarak benzer. Farklar: [7]'de geri yüklemeyi sunucu başlatır ve değiştirilen tek ayar parça boyutudur; bizde eşitleme isteğini cihaz başlatır ve boşaltmanın zamanlaması ile sırası (B0-B3) politika olarak değiştirilir. B3'ün dayandığı öncelikli ve hız sınırlı geri doldurma fikri [8]'de tek bir geçit için simüle edilmiştir.

### 3.3 Kesinti senaryosu

Cihazlar 20 sn içinde yayılarak bağlanır (başlangıç fırtınası olmasın diye). Başlangıçtan 40 sn sonra sunucu konteyneri durdurulur (temiz kesinti: bağlantılar sonlanır, cihazlar kopmayı hemen öğrenir), 60 sn sonra yeniden başlatılır ve sonrası izlenir: bağlanma aşaması deneylerinde (Bölüm 5.1-5.3) 90 sn, TLS'li bağlanma deneyinde (5.4) 150 sn, bağlantı sonrası yük deneylerinde (5.5 ve sonrası) 90-180 sn (uzun pencereli boşaltmanın bitebilmesi için). Bu temel senaryodan ayrılan iki durum Bölüm 5.11'de (sessiz kesinti: ağın sessizce kopması) ve Bölüm 5.12'de (15 ve 180 sn kesinti) incelenmiştir.

### 3.4 Ölçütler

Toparlanma süresi (TTR): sunucunun başlatıldığı andan bağlı cihazların %50, %95 ve %100'üne ulaşılana kadar geçen süre (çözünürlük 1 sn). Kesintideki ve sonrasındaki tepe bağlantı denemesi/sn. Sunucu açıkken başarısız bağlantı sayısı. Sunucu ve simülatör CPU'su (~0,7 Hz örnekleme). Bağlantı sonrası yük deneylerinde: (i) **canlı alarm gecikmesi**: toplayıcının mesajı aldığı an eksi mesajın *planlanan* gönderim anı (açık döngü, saniyelik p99); *taban* kesinti öncesi saniyelik p99'ların medyanı, *tepe* sunucu açıldıktan sonraki saniyelik p99'un en büyüğü, ayrıca p99'un 100 ms'yi aştığı saniye sayısı; (ii) **boşaltma bitişi**: sunucunun açıldığı andan son biriken mesajın toplayıcıya ulaşmasına kadar geçen süre; (iii) **eşitleme süresi** (istek-yanıt, p50 ve p99); (iv) sunucu CPU tepesi; (v) kayıp ve tekrar sayısı (mesaj numaralarıyla); (vi) **birikmiş alarmın yaşı**: kesintide biriken bir alarmın toplayıcıya ulaştığı an eksi planlanan üretim anı (p50, p95 ve en büyük; saniye başına yüzdeliklerin adet ağırlıklı hesabı, yaklaşık). Gecikme, iki konteynerin ortak çekirdek saati (CLOCK_MONOTONIC) ile ölçülür (Bölüm 4.3). Her hücre 3 tekrarla koşulmuş, **medyan ve en küçük-en büyük aralık** raporlanmıştır; güven aralığı iddiası yoktur.

### 3.5 Deney ortamı

**Tablo 0.** Donanım ve yazılım (kaynak: `00_ortam.md`; EMQX Bölüm 5.9 için eklenmiştir).

| Öğe | Değer |
|---|---|
| İşlemci | Intel Core i7-11600H @ 2,90 GHz, 6 çekirdek / 12 mantıksal |
| Bellek | 15,7 GB |
| İşletim sistemi | Windows 11 Home Single Language 10.0.26200, güç planı Yüksek performans |
| Konteyner ortamı | Docker Desktop (Docker Engine 28.4.0), WSL2; Docker'a açık kaynak 12 CPU, yaklaşık 8,2 GB; `.wslconfig` yok (varsayılan) |
| Sunucular | Eclipse Mosquitto 2.1.2 (`eclipse-mosquitto:2`); EMQX 5.8.6 (`emqx/emqx:5.8`, imza `sha256:a1e3d10f…`) |
| İstemci kütüphanesi | MQTTnet 5.2.0.1603, .NET 8, MQTT 5.0 |
| Konteyner payları | Sunucu 2 CPU / 2 GB (EMQX ayrıca 4 CPU); simülatör 4 CPU / 3 GB; toplayıcı 2 CPU / 1 GB |

Sonuçlar bu makineye göreceli raporlanır. Koşular sırasında Docker'da başka bir projeye ait iki boşta konteyner çalışmaktaydı; etkisi ölçülmemiştir.

### 3.6 Kayıt ve tekrarlanabilirlik

Her koşu bir girdi dosyası (`girdi.json`), saniyelik ham çıktı (CSV), kesinti zamanları ve kod sürümü (git commit) ile kayıt defterine işlenir. Özet sayılar elle değil betikle üretilir. Hatalı koşular silinmez, geçersiz olarak işaretlenir.

## 4. Ölçüm Ortamının Doğrulanması

Bu bölüm, ölçüm düzeneğinin kendisinin sonuçları nasıl çarpıtabildiğini ve bunların nasıl bulunup düzeltildiğini anlatır. Hatalı koşular silinmemiş, geçersiz olarak işaretlenmiştir.

- **4.1 Zaman aşımı hatası.** İlk dört koşuda (N = 500-5.000, sürüm v1) "fırtına çıkmadı" gibi görünüyordu (sunucu CPU'su %1-39, toparlanma 4-5 sn). Saniye saniye deneme sayısına bakınca kesinti sırasında neredeyse hiç yeniden deneme olmadığı görüldü: 5.000 cihaz bir kez denedi, sonra 60 sn boyunca toplam yaklaşık 15 deneme. Neden: MQTTnet'in `WithTimeout` ayarı, yanıt vermeyen (durdurulmuş) sunucunun SYN'ine cevap gelmeyen TCP bağlantı kurulumunu sınırlamıyordu; cihazlar tek bir asılı denemede bekledi ve bekleme kuralı hiç çalışmadı. Düzeltme: `ConnectAsync` çağrısına kendi zaman aşımımız (`CancelAfter`, 5 sn) eklendi. (günlük `04`, Bulgu 1; koşular `adim3_*_v1` geçersiz işaretli)
- **4.2 Ad çözümlemesi artefaktı.** Cihazlar sunucuya adıyla (`broker`) bağlandığında 5.000 cihazın toparlanması 27-69 sn sürdü; IP ile bağlandığında 2,1 sn (TTR %50: 1,1 sn). İki aday açıklama sınanıp reddedildi: sunucu MAC adresinin değişmesi (sabit MAC ile gecikme sürdü) ve çekirdekte dinleme kuyruğunun taşması (`ListenOverflows`, `ListenDrops`, `TCPReqQFull*`, `TCPSynRetrans` hep 0; sunucu her bağlantıyı yalnızca bir kez kabul etmişti, başarısız denemeler sunucuya hiç ulaşmamıştı). Gecikme, Docker'ın dahili ad çözümleyicisinin eşzamanlı çok sayıda sorguda yavaşlamasıyla uyumludur; mekanizma doğrudan gözlenmedi. Bundan sonra tüm koşular IP ile yapılmıştır; ad ile yapılan koşular (sürümler v2, v2b, v3mac) artefakt içerir. (günlük `04`, Bulgu 5)
- **4.3 Saat sıçraması.** Bağlantı sonrası yük deneylerinin ilk pilotunda canlı alarm gecikmesi bazı saniyelerde negatif çıktı (−2,3 sn, −4,7 sn); bir mesajın planlanan andan önce alınması imkânsız olduğundan bu bir ölçüm hatasıydı. İnceleme iki neden gösterdi. Birincisi, cihaz zamanlayıcısı (`Task.Delay`) mesaj zamanından önce uyanıyor ve kod uyanma sonrası kontrol yapmıyordu; kontrol eklendi. İkincisi, **WSL2 sanal makinesinin duvar saati yaklaşık her 30 sn'de bir ani sıçrıyor**: kayıtlı saat bekçisi, örneğin +3.011 ms ve 100 ms sonra −3.487 ms sıçramaları yakaladı. Kalıcı çözüm olarak planlanan ve alınan anlar tek yönlü saatle (CLOCK_MONOTONIC) ölçüldü; 1.560 mesajlık doğrulama koşusunda, iki saat sıçramasına rağmen gecikme 0-20 ms çıktı. Duvar saatiyle yapılan konteynerler arası gecikme ölçümleri bu ortamda bozulabilir. (günlük `06`, Bulgu 1)
- **4.4 Sunucu ayarlarının ölçüme etkisi.** EMQX'in varsayılan koruması (`force_shutdown`; istemci işleminin posta kutusu 1.000 mesajı aşarsa bağlantıyı kes), boşaltma sırasında tek toplayıcı aboneyi `mailbox_overflow` ile defalarca koparmış; toplayıcı kopunca 272.526 mesaj (üretilenin %43'ü) ve eşitleme yanıtları kaybolmuştur (pilot). Ayar, Mosquitto'da kuyruk sınırının kaldırılmasına eşdeğer bir ölçüm ayarı olarak kapatılmıştır; yani gecikme ölçümünün kendisi sunucunun varsayılan koruma davranışıyla çakışabilir ve bu, gerçek bir dağıtımda yoğun bir abonenin boşaltma anında koparılacağı anlamına gelir. Ayrıca bir istemci kodu değişikliği (TLS seçeneğinin TLS'siz modda da açık kalması) bir EMQX pilotunda hiçbir cihazın bağlanamamasına yol açmış, fark edilip düzeltilmiş; etkilenen geçerli koşu yoktur (günlük `12`).
- **4.5 Çıkarım.** Toplam "bağlandı" sayısı yetmez; kesinti sırasındaki deneme sayısı, kayıp ve tekrar sayısı, kullanılan saat ve sunucunun varsayılan koruma davranışları ayrıca doğrulanmalıdır. Ölçüm ortamının kendisi "fırtına"yı üretebilir ya da gizleyebilir.

## 5. Bulgular

*Not (yazım): 5.1-5.3 (sürüm v5), 5.4 (t1), 5.5-5.7 (v6, v7, v8), 5.8 (tls6), 5.9 (e1, e2), 5.10 (p1, p2), 5.11 (s9) ve 5.12 (k10) yazıldı. Son sürümde bu not silinecek.*

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

### 5.10 Alarm önceliği ve birikmiş alarmın yaşı (sürümler p1, p2)

Şimdiye kadar yalnızca *canlı* alarmın gecikmesine bakıldı. Klinik açıdan ikinci bir soru da vardır: kesintide üretilmiş (biriken) bir alarm sunucuya ne kadar geç ulaşıyor? B2 gibi boşaltmayı zamana yayan politikalar canlı alarmı korurken birikmiş alarmı da bekletir. Önceki matrislerin verisinden hesaplanan **birikmiş alarmın yaşı** (Bölüm 3.4) rastgeleli kuralda B1'de medyan 37-42 sn, B2'de 56-94 sn çıkmıştır (üstel kuralda 56-72 ve 64-74 sn; en büyük değer: N = 10.000'de uzun pencereli B2'de yaklaşık 174 sn, kesintinin kendisi 60 sn). B3 (Bölüm 3.2) bu bedeli düşürmek için tasarlanmıştır. Hipotezler (HP1-HP5) koşulardan önce yazılmıştır (`13_adim8_alarm_onceligi.md`). İki koşul koşulmuş, her matriste B2 ve B3 aynı blokta yinelenmiştir: **P1** (Mosquitto, TLS'siz, N = 10.000, rastgeleli kural, B1 / B2 / B3 pencere 90 sn, aralık 500 ms; 9 koşu) ve **P2** (Mosquitto, TLS, N = 5.000, üstel ve rastgeleli kural, B2 / B3 30 sn / 200 ms; 12 koşu).

**Tablo 10.** Canlı alarm tepesi ve birikmiş alarmın yaşı, medyan [en küçük-en büyük] (yaş: p50 / en büyük, sn). Kaynak: `results/ozet_adim5_p1_hucreler.csv`, `ozet_adim5_p2_hucreler.csv`.

| Koşul | A | B | Alarm p99 tepe (ms) | Boşaltma bitişi (sn) | Birikmiş alarmın yaşı (p50 / en büyük) |
|---|---|---|---|---|---|
| P1, N = 10.000, TLS'siz | rastgeleli | B1 | 6.851 [6.293-8.047] | 34,5 | 41,8 / 96,4 |
| | rastgeleli | B2 90/500 | 517 [354-1.442] | 140,4 | 93,7 / 174,1 |
| | rastgeleli | B3 90/500 | 210 [112-280] | 135,9 | 37,3 / 90,3 |
| P2, N = 5.000, TLS | üstel | B2 | 673 [557-1.177] | 108,0 | 71,0 / 153,1 |
| | üstel | B3 | 1.524 [1.341-2.184] | 105,7 | 53,9 / 125,6 |
| | rastgeleli | B2 | 80 [77-94] | 68,0 | 56,0 / 117,6 |
| | rastgeleli | B3 | 87 [60-623] | 64,5 | 36,7 / 89,8 |

`[Şekil: sekil_b3_denge.png]`

**Alarmı öne almak, birikmiş alarmın yaşını B1 düzeyine indirir ve canlı alarmı B2 kadar korur.** Rastgeleli kuralda B3, birikmiş alarmın yaş medyanını B2'ye göre %60 (N = 10.000; 93,7 → 37,3 sn) ve %34 (TLS; 56,0 → 36,7 sn), en büyük yaşı %48 ve %24 azaltır; bu değerler B1'in yaşıyla (≈ 38-42 sn) aynı düzeydedir. Canlı alarm tepesi B2'den kötüleşmez (210 vs 517 ms; 87 vs 80 ms), vital mesajların boşaltma bitişi de aynı kalır (%2-5 fark). Birikmiş alarmın yaşının alt sınırı yeniden bağlanma süresidir (alarm, cihaz dönmeden gönderilemez); B3'te alarm boşaltması dönüş penceresinin sonunda (yaklaşık 29 sn) biter.

**Üstel dönüş ve TLS'te B3'ün bedeli vardır.** Senkron dönüşte yaklaşık 30 bin birikmiş alarm aynı anda gönderilir ve el sıkışma yükünün üstüne biner: canlı alarm tepesi B2'ye göre 2,3 kat yükselir (1.524 vs 673 ms) ve kazanç küçük kalır (yaş medyanı %24, en büyük yaş %18 azalır). P1'de B3'ün canlı alarm tepesinin B2'den de düşük çıkması beklenmedikti (3 tekrarda aralıklar örtüşmüyor) ve nedeni incelenmemiştir. Hipotezlerin yalnızca HP4 (boşaltma bitişi farkı ≤ %10) tam desteklenmiştir; HP1 (yaş medyanı ≥ %25 düşük), HP2 (en büyük yaş ≥ %25 düşük) ve HP3 (canlı alarm tepesi ≤ 2 kat) kısmen, HP5 (kayıp ≤ 1) kısmen desteklenmiştir.

**Bloklar arası değişkenlik büyüktür.** Aynı hücre (rastgeleli + B2 90 sn / 500 ms, N = 10.000, TLS'siz) Bölüm 5.7'de 169 ms [154-198], burada 517 ms [354-1.442] çıkmıştır; B1 6.157 ve 6.851 ms. Bu nedenle bu bölümde karşılaştırılan hücreler aynı blokta koşulmuş, farklı sürümler arası karşılaştırma yalnızca yön göstergesi sayılmıştır.

*Kayıp ve tekrar:* tekrar 21 koşunun hepsinde 0; kayıp bir koşuda (P1, B3, tohum 3) 42 mesajdır, diğerlerinde 0.

*Sınırlar:* iki koşul ve tek sunucu (EMQX'te B3 denenmemiştir); hücre başına 3 tekrar; alarm ve vital ayrımı ile alarm sıklığı varsayımsaldır; üstel + TLS'te B3 bedelinin ve P1'deki beklenmedik düşüşün mekanizması test edilmemiştir; yaş yüzdelikleri saniye başına yüzdeliklerden hesaplanmış yaklaşık değerlerdir.

### 5.11 Sessiz kesinti (sürüm s9)

Bölüm 5.5'ten itibaren kesinti temizdi: sunucu kapanıyor, bağlantılar anında sonlanıyordu. Gerçek kesintilerin çoğu sessizdir (ağ kopar, soket kapanmaz). Bu bölümde sunucu, kapatılmak yerine Docker ağından koparılmış (60 sn sonra aynı IP ile geri bağlanmıştır) ve iki kesinti türü aynı blokta karşılaştırılmıştır: kesinti türü (temiz, sessiz) × bekleme kuralı (üstel, rastgeleli üstel) × B1, N = 5.000, Mosquitto, TLS'siz, 3 tekrar, 12 koşu (`15_adim9_sessiz_kesinti.md`). Hipotezler (HS1-HS5) koşulardan önce yazılmış, çalışma testlerinin sonucu yazım sırasında biliniyordu. **Önemli bir koşul:** çalışma testinde, yayın onayı (PUBACK) beklerken zaman aşımı sonrası bağlantıyı kapatmayan bir istemcinin sessiz kesintiyi 40 sn boyunca hiç fark etmediği görüldü (bağlı sayısı değişmedi): sürekli yayın yapan cihazda kalp atışı kontrolü tetiklenmiyor ve MQTTnet yayın zaman aşımını hata olarak veriyor ama bağlantıyı kapatmıyor. Bu yüzden bu matriste yayın onayı zaman aşımında bağlantıyı kapatma seçeneği **her iki kesinti türünde de** açıktır; Bölüm 5.5-5.10'daki koşularda kapalıydı (temiz kesinti kolunun Bölüm 5.5 ile uyumu bir sağlama olarak görüldü: üstel + B1 tepesi 9.176 ve 9.158 ms).

**Tablo 11.** Sessiz ve temiz kesinti, N = 5.000, Mosquitto, TLS'siz, B1 (medyan [en küçük-en büyük]). Kaynak: `scripts/analiz_adim5.py s9`, `results/adim5_*_s9.*`.

| Kesinti | A | Alarm p99 tepe (ms) | >100 ms süre (sn) | Cihaz tarafı gönderim hatası | Kopmayı fark etme |
|---|---|---|---|---|---|
| temiz | üstel | 9.176 [9.167-9.256] | 19 [19-20] | 433 [322-569] | ≈ 1 sn |
| sessiz | üstel | 5.858 [5.843-6.258] | 15 [14-16] | 35.020 [34.705-35.024] | ≈ 11 sn |
| temiz | rastgeleli | 66 [54-69] | 0 | 423 [325-616] | ≈ 1 sn |
| sessiz | rastgeleli | 638 [573-769] | 4 [3-4] | 34.958 [34.869-35.115] | ≈ 11 sn |

**Sessiz kesinti, beklenen dağınık fark etmeyi yaratmadı.** Cihazlar kopmayı kesintiden yaklaşık 11 sn sonra fark etti ama hepsi birlikte: bağlı sayısı %95'ten %5'e 1-2 sn'de indi (temiz kesintide anında); yani izdiham dağılmadı, yalnızca 11 sn kaydı (HS1 çürüdü). Üstel + B1'de tepe %36 düştü (9,2 → 5,9 sn; HS2'deki %50 eşiği sağlanmadı); bu, bir dağılmadan çok geri çekilme takviminin kaymasına benziyor (cihazlar kesintiyi geç fark edince takvim aynı kadar kayar ve sunucu açıldıktan kısa süre sonra bir deneme dalgası denk gelir), ancak bu açıklama test edilmedi. **Rastgeleli + B1'de ise sessiz kesinti durumu kötüleştirdi:** tepe 66 ms'den 638 ms'ye çıktı (HS3 sessizde çürüdü) ve boşaltma tepe hızı 14,7 bin'den yaklaşık 24 bin mesaj/sn'ye yükseldi. Olası açıklama (test edilmedi): sessiz kesintide başarısız denemeler anında reddedilmek yerine zaman aşımına uğruyor ve ağ geri gelince bekleyen bağlantı isteklerinin yeniden iletimleri eşzamanlı tamamlanıyor. Bedel olarak cihazlar kopmayı fark edene kadar yayın yapmaya devam ediyor: koşu başına yaklaşık 35 bin cihaz tarafı gönderim hatası (temiz kesintide 322-616; HS4 desteklendi); mesaj kaybı yok (en çok 1; HS5).

*Sınırlar:* sessiz kesinti Docker ağından kopma ile taklit edilmiştir (tek yönlü paket kaybı, kısmi kopma ve gecikme dalgalanması yok); yalnızca B1, N = 5.000, Mosquitto ve TLS'siz denenmiştir; B2/B3, TLS ve EMQX sessiz kesintide denenmemiştir. Yayın onayı zaman aşımının açık olması istemci davranışının bir parametresidir ve Bölüm 5.5-5.10 ile doğrudan karşılaştırmayı sınırlar. Süre kaymasının ve rastgeleli kuraldaki kötüleşmenin nedenleri test edilmemiştir; hücre başına 3 tekrar vardır.

### 5.12 Kesinti süresi (sürüm k10)

Önceki bölümlerde sunucu hep 60 sn kapalı kaldı. Kapalı kalma süresi hem biriken mesaj miktarını (süreyle orantılı) hem de üstel beklemenin hangi aşamada olduğunu değiştirdiği için, bulguların yalnızca bu süreye özgü olup olmadığı ayrıca sınanmıştır: kesinti süresi (15, 60, 180 sn) × bekleme kuralı (üstel, rastgeleli üstel) × B1, N = 5.000, Mosquitto, TLS'siz, temiz kesinti, 4 tekrar, 24 koşu (`16_adim10_kesinti_suresi.md`). Hipotezler (HK1-HK5) ve web gezginindeki modelin tahmini koşulardan önce yazılmıştır; model bu sürelerde hiç kalibre edilmediği için gerçek bir sınav oldu.

**Tablo 12.** Kesinti süresi, N = 5.000, Mosquitto, TLS'siz, B1 (medyan [en küçük-en büyük], 4 tekrar). Kaynak: `scripts/analiz_adim5.py k10`, `results/adim5_*_k10.*`.

| A | Kesinti (sn) | Alarm p99 tepe (ms) | >100 ms süre (sn) | Sunucu CPU tepe (%) | Model tahmini, tepe (ms) |
|---|---|---|---|---|---|
| üstel | 15 | 1.948 [1.575-2.193] | 6 [4-8] | 83 | 9.255 |
| üstel | 60 | 9.066 [8.634-9.870] | 19,5 [18-21] | 98 | 9.255 |
| üstel | 180 | 20.033 [19.438-21.030] | 38,5 [37-40] | 100 | 12.530 |
| rastgeleli | 15 | 242 [157-433] | 2 [1-4] | 83 | 395 |
| rastgeleli | 60 | 49 [42-53] | 0 | 84 | 223 |
| rastgeleli | 180 | 1.529 [1.220-2.386] | 22 [10-27] | 96 | 832 |

**Üstel kuralda tepe, kesinti süresiyle neredeyse doğrusal arttı** (kesinti saniyesi başına 0,11-0,15 sn; HK1 desteklendi): 15 sn'de 1,9 sn, 60 sn'de 9,1 sn, 180 sn'de 20,0 sn. Bu, cihazların toplam 3.000 mesaj/sn ürettiği, kesintide biriken miktarın bu hızla süreyle orantılı büyüdüğü ve sunucunun tepede yaklaşık 20-27 bin mesaj/sn boşalttığı basit hesabıyla uyumludur (sonradan yapılmış bir açıklamadır, ayrıca sınanmamıştır). 180 sn'de sunucu doymuştur (CPU %100, 676-1.101 eşitleme zaman aşımı; 60 sn'de hiç yok). **Rastgeleli kural her süre için üsteli geçti** (tepe oranı 8× ile 185× arasında; HK3), ancak tepe süreyle tekdüze değildir: 15 sn'de 242 ms, 60 sn'de 49 ms, 180 sn'de 1.529 ms. 180 sn'deki artış, biriken işin (yaklaşık 540 bin mesaj) 30 sn'ye yayılsa da sunucu kapasitesine yaklaşmasıyla uyumludur (CPU %96); yani **rastgele dağıtmanın sağladığı güvenli bölge kesinti süresiyle daralır** (HK2 desteklendi). 15 sn'deki beklenmedik yüksek değerin nedeni açıklanamamıştır; olası ama sınanmamış bir neden, bekleme penceresi henüz tavana ulaşmadığı için dönüşün daha dar bir aralığa yayılmasıdır.

**Model bu sınavın yarısını geçti.** Altı hücrenin dördünde tepeyi 2 kat içinde buldu (HK5; ölçütün alt sınırı), ancak kaçırdığı yer önemlidir: model, üstel kuralda tepenin 15 sn'de de 60 sn'de de aynı (≈ 9,3 sn) olacağını öngördü, ölçüm 1,9 ve 9,1 sn verdi (4,8 kat sapma). Bu yüzden modeli 60 sn dışındaki kesinti sürelerinde kullanmak güvenilir değildir; rastgeleli 60 sn hücresindeki 4,5 katlık sapma ise adım 5'ten beri bilinen bir sapmadır.

**Kayıp.** 24 koşunun 20'sinde sıfır; rastgeleli 15 sn hücresinde iki koşuda 255 ve 314, rastgelelide 180 sn ve üstelde 15 sn'de birer koşuda 2 ve 1 mesaj ulaşmadı (HK4 çürüdü). 255 ve 314, bu çalışmanın Mosquitto koşularında (Bölüm 5.5'ten itibaren) görülen en büyük kayıplardır (öncekinin en büyüğü 132; yine de en çok yaklaşık %0,07). İki koşuda da çok sayıda cihaz tarafı eşitleme zaman aşımı vardı (1.214 ve 1.408), ancak sıfır kayıplı üstel 180 sn koşularında da 676-1.101 zaman aşımı olduğundan bunlar nedeni açıklamıyor. Ham kayıt açık olmadığı için neden araştırılamadı.

*Sınırlar:* yalnızca B1, Mosquitto, TLS'siz, N = 5.000 ve temiz kesinti; üç kesinti süresi vardır (aradaki ve daha uzun süreler denenmemiştir; 180 sn'de üstel kural 30 sn tavanındadır); hücre başına 4 tekrar vardır.

## 6. Tartışma ve Sınırlar

### 6.1 Ana bulgular

1. **Bağlantı sonrası iş, bağlanmanın kendisinden daha büyük bir yük olabilir.** TLS'siz ortamda 5.000 cihazın aynı anda dönmesi Mosquitto'yu zorlamamış (CPU en çok 30 / 200; Bölüm 5.3), ama dönüşün ardından yapılan eşitleme ve birikenin boşaltılması, üstel kuralla canlı alarm gecikmesini yaklaşık 30 ms'den yaklaşık 9 sn'ye çıkarmıştır (Bölüm 5.5). Bu, bağlanma yükünü azaltmaya odaklanan yöntemlerin ([1], [2]) yeterli olmayabileceğine işaret eder: bağlandıktan sonraki iş de zamanlanmalıdır.
2. **Yük arttıkça işe yarayan politika değişir; "en iyi politika" ortama bağlıdır.** TLS'siz N = 5.000'de dönüşü rastgele dağıtmak yeterlidir (51 ms); N = 10.000'de ya da TLS'te aynı hücre doyar (6,2 sn ve 1,2 sn) ve boşaltmayı da zamana yaymak (B2) gerekir, ayarı yüke göre büyütülerek (Bölüm 5.6-5.8). Bedeli boşaltmanın uzamasıdır (yaklaşık 1,5-4 kat) ve kesintide biriken verinin gecikmesidir (Bölüm 5.10).
3. **Rastgeleli kuralın faydası ancak sunucu zorlandığında görünür.** TLS'siz ortamda rastgeleli kural yalnızca bedel getirmiş (toparlanma uzamış), TLS'te toparlanmayı yarıdan fazla kısaltmış ve başarısız bağlantıyı sıfırlamıştır (Bölüm 5.4). Sunucu zorlanmadığı sürece "gereksiz" görünen bir mekanizmanın değeri yük arttığında ortaya çıkar.
4. **Bulgular tek bir sunucuya ya da çekirdek sayısına özgü değildir, ama büyüklükleri öyledir.** Çok çekirdekli EMQX'te de ikinci izdiham görülmüş, politika sıralaması aynı kalmış; çekirdek sayısını ikiye katlamak (2'den 4'e) gecikmeyi yalnızca %29 azaltmıştır (Bölüm 5.9). Aynı CPU payında EMQX her hücrede Mosquitto'dan yavaş çıkmıştır.
5. **Ölçümün kendisi dikkat ister.** Zaman aşımı hatası, ad çözümlemesi, saat sıçraması ve sunucunun varsayılan koruma ayarı sonuçları çarpıtabilecek düzeydeydi (Bölüm 4); hepsi gecikme ölçümlerinden önce yakalanıp düzeltildi.
6. **Kesintinin süresi ve türü sonucu değiştirir.** Üstel kuralda tepe gecikme kesinti süresiyle neredeyse doğrusal artar (15, 60, 180 sn: 1,9; 9,1; 20,0 sn) ve rastgeleli kuralın güvenli bölgesi uzun kesintide daralır (180 sn'de 1,5 sn; Bölüm 5.12). Ağ sessizce koptuğunda cihazlar kopmayı yaklaşık 11 sn geç ama eşzamanlı fark eder, izdiham dağılmaz ve rastgeleli kuralda tepe 66 ms'den 638 ms'ye çıkar (Bölüm 5.11). Yani 60 sn'lik temiz kesintide edinilen "rastgeleli kural yeterlidir" sonucu, kesinti uzadığında ya da sessiz olduğunda geçerli kalmayabilir.
7. **Basit bir model bu bulguların yalnızca bir kısmını yakalar.** Web gezgininde kullanılan basit model, 60 sn'lik kesintide kalibre edildiği hücrelerde tutmuş, kalibre edilmediği 15 ve 180 sn'lik sürelerde altı hücrenin dördünde ölçümü 2 kat içinde bulmuş, üstel kuralda 15 sn'de ise 4,8 kat sapmıştır (Bölüm 5.12). Model bu yüzden tahmin aracı olarak değil, bir sunum aracı olarak kullanılmıştır.


### 6.2 Bulguların ilgili çalışmalarla ilişkisi

Aşağıdaki karşılaştırmalar farklı sistemler arasındadır; bu yüzden "doğrular" ya da "çürütür" değil, "uyumlu" ya da "farklı" biçiminde okunmalıdır.

**Birikmiş veri ve sunucu kuyruğu [7].** Domingues ve ark., tek bir istemcide bile çok küçük parçaların (saniyede yaklaşık 300 mesaj) Mosquitto'da kuyruk yarattığını bildirir. Bizim bulgumuz, çok sayıda cihazın birikmiş küçük mesajları aynı anda boşaltmasının aynı mekanizmayı büyük ölçekte tetiklediği yönündedir; ikisi uyumludur, ancak biz mekanizmayı doğrudan sınamadık. [7] canlı verinin geri yüklemeyi etkilemediğini bildirir; biz ters yönü, yani boşaltmanın canlı alarmı geciktirdiğini ölçtük. Çelişki yoktur: [7]'de canlı akış geri yükleme akışının çok altındadır (yaklaşık 0,175 ve 8,3 Mbps) ve tek istemci vardır; bizde binlerce cihazın birikmiş akışı aynı sunucu kuyruğunda canlı akışla yarışır. Domingues ve ark.'nın en uygun parça boyutunu bulması ile bizim zamanlama ve sıra politikalarını (B2, B3) incelememiz birbirini tamamlar; parça boyutu ve zamanlamanın birlikte ayarlanması denenmemiştir.

**Rastgelelik ve eşzamanlılık [1]-[3].** Bulgularımız, standartların önerdiği rastgele gecikmenin değerini sunucu zorlandığında gösterir (Bölüm 5.4) ve rahat ortamda yalnızca bedel getirdiğini ekler (Bölüm 5.2). Floyd ve Jacobson'ın "gereken rastgelelik şaşırtıcı derecede büyüktür" bulgusu [3], rastgeleli kuralın güvenli bölgesinin uzun kesintide daralmasıyla (Bölüm 5.12) tutarlı olabilir: biriken iş arttıkça aynı pencere yetmemiş olabilir. Bunu doğrudan sınamadık; rastgelelik penceresinin genişliğini ve tavanını taramadık, kuralımız tek ayardır (`U(0,5·W, W)`, W ≤ 30 sn; Bölüm 3.2). Ayrıca [3] sürekli periyodik mesajları inceler; bizdeki eşzamanlama, kesintinin herkesi aynı anda başlattığı tek seferlik bir olaydır. Aynı çalışmanın istemcilerin sunucu toparlanırken kurtarma işlemlerinde eşzamanlı hâle gelmesi örneği [3, §1] ise bizim durumumuza kavramsal olarak en yakın örnektir.

**Tetikleyici süresi ve metastabilite [4], [5].** Kesinti süresi uzadıkça gecikmenin büyümesi, Huang ve ark.'nın "daha uzun tetikleyici sistemi daha kırılgan yapar" bulgusuyla uyumludur [4, §3.5]. Ancak bizim ölçümlerimizde aşırı yük geçicidir: birikim boşaldığında canlı gecikme taban değere döner ve tetikleyici kalktıktan sonra kalıcı bir çöküş gözlenmemiştir; TLS'teki "ikinci dalga" da toparlanmıştır. Bu nedenle sonuçlarımızı metastabil çöküş olarak adlandırmıyoruz. Bizim durumumuzdaki yükseltme, yeniden deneme değil, kesintiyle orantılı biriken işin eşzamanlı boşaltılmasıdır. Isaacs ve ark.'nın modelin nicel tahminlerinin sapabileceği ve gerçek sistemde doğrulamanın şart olduğu uyarısı [5, §3, §6], modelimizin 15 sn'deki sapmasıyla örtüşür.

**Hücresel çekirdekte aynı mekanizma ailesi [6].** Spillway'in anlattığı zincir (eşzamanlı yeniden bağlanma, kuyruk, cihazın zaman aşımından sonra işlenen boşuna iş, yeniden deneme) [6, §2.3] bizim ikinci dalga bulgumuzla aynı aileden görünür; ancak bizde zaman aşımına uğrayan el sıkışma işinin kuyrukta kalmasını doğrudan ölçmedik, yalnızca modelde varsaydık. İki çalışmanın kontrol noktası farklıdır: [6] sunucu tarafında yük atarak (statik hız sınırları), biz cihaz tarafında zamanlayarak (bekleme kuralı ve boşaltma politikası) etki eder. Sunucu tarafı hız sınırı, bu çalışmada denenmemiştir (Bölüm 6.5). Spillway'in sunucu tarafı yaklaşımı ile bizim cihaz tarafı yaklaşımımız rakip değil tamamlayıcı olabilir; bunu sınamadık. Ayrıca [6]'nın sayısal ikizi geçmiş olaylarla doğrulanmıştır; bizim modelimiz bu düzeyde doğrulanmamıştır.

**Öncelikli boşaltma [8].** Bi ve ark.'nın simülasyonunda öncelik, yüksek öncelikli olayların bitişini belirgin biçimde öne çekmiş, hız sınırı ise patlamayı azaltmıştır. Bizim ölçümlerimizde B3 birikmiş alarmın yaşını B2'ye göre %24-60 düşürmüş, ama üstel dönüş ve TLS'te canlı alarmı B2'ye göre 2,3 kat kötüleştirmiştir (Bölüm 5.10). Öncelik fikrinin faydası bu nedenle ortama bağlıdır; [8]'in tek geçitli, canlı gecikmesiz simülasyonu bu etkileşimi göremezdi. Bu iki sonuç farklı sistemler arasındadır ve doğrudan karşılaştırılmamalıdır. [8]'deki tekrar önleme (dizi numarası, toplu onay) ve sınırlı tampon için öncelikli saklama bizde yoktur (Bölüm 6.5).

**MQTT 5'te sunucu ipucunun yokluğu [2], [6], [9].** MQTT 5'te sunucu yoğunluğu neden koduyla bildirebilir ama ne kadar bekleneceğini bildiremez (Bölüm 2.4). Bizim bulgumuz, bekleme kuralının sonucu kökten değiştirdiğini gösterir (N = 5.000, 60 sn, B1: üstel 9.066 ms, rastgeleli 49 ms; Bölüm 5.12). Bu, zamanlamanın tamamen istemci politikasına bırakıldığı bir protokolde, istemci tarafı kuralın önemini vurgular. Sunucunun bir bekleme süresi önermesinin (SIP'teki `Retry-After` gibi [2]) bu farkı azaltıp azaltmayacağı bu çalışmada denenmemiştir.

### 6.3 Uygulamaya dönük ön çıkarımlar

Aşağıdakiler bu ortamdaki gözlemlerden çıkan ön önerilerdir, genel kural değildir (bkz. 6.4-6.5):

- Sunucunun zorlandığı yük bölgesine yaklaşılıyorsa (TLS, çok sayıda cihaz), dönüşü rastgele dağıtmak (rastgeleli geri çekilme) en ucuz ve çoğu zaman yeterli ilk önlemdir.
- Bu yetmiyorsa bağlantı sonrası işi de zamana yaymak (cihaz başına rastgele gecikme ve aralıklı boşaltma) gerekir; parametre (pencere, aralık) cihaz sayısıyla ve toplam yükle büyütülmelidir. Sabit bir ayar ölçek büyüyünce yetmeyebilir (Bölüm 5.6-5.7).
- Çekirdek eklemek tek başına çözüm değildir (Bölüm 5.9).
- Biriken alarmlar azsa ve ayrıştırılabiliyorsa, boşaltmayı yayarken alarmları öne almak (B3) birikmiş alarmın yaşını düşürür ve canlı alarmı korur; ancak senkron dönüşte (üstel kural, TLS) bu alarm yığını canlı alarmı kötüleştirebilir (Bölüm 5.10).
- Boşaltmayı geciktirmek kesintide biriken verinin (ve alarmların) ulaşmasını geciktirir; bu bedel ayrıca değerlendirilmelidir (Bölüm 5.10).
- Kesinti uzadıkça dönüşü dağıtma penceresi ve boşaltmanın yayılımı da büyütülmelidir: 180 sn'lik bir kesintide 5.000 cihazda rastgeleli kural ve B1 yetmemiştir (Bölüm 5.12).
- Ağın sessizce kopabileceği ortamlarda istemci yayın onayı için bir zaman aşımı kullanmalıdır; yoksa cihaz kopmayı hiç fark etmeyebilir (Bölüm 5.11).
- Test ederken toplam bağlı sayısı yetmez; deneme sayısı, kayıp, canlı trafik gecikmesi ve ölçümün saat kaynağı doğrulanmalı; sunucunun varsayılan koruma ayarları (ör. EMQX `force_shutdown`) kontrol edilmelidir.

### 6.4 Geçerlilik tehditleri

**İç geçerlilik.** (i) Hücre başına 3 tekrar vardır (Bölüm 5.12'de 4); medyan ve aralık verilmiş, güven aralığı iddia edilmemiştir; aykırı koşular çıkarılmamıştır. (ii) Karşılaştırılan sürümler (ör. TLS'siz ve TLS'li, Mosquitto ve EMQX) aynı blokta koşulmamıştır; zamanla değişen etkiler (ısınma, arka plan) tam kontrol edilmemiş, bloklar arası değişkenliğin büyük olabildiği görülmüştür (aynı hücre iki blokta 169 ve 517 ms; Bölüm 5.10). Bu yüzden sürümler arası karşılaştırmalar yön göstergesi olarak okunmalıdır. (iii) Ölçüm düzeneği (toplayıcı, simülatör) aynı makinedeki kaynağı paylaşır; simülatör ve host CPU doygunluğu bazı koşullarda gecikmeye karışmış olabilir (Bölüm 5.3, 5.6, 5.9). (iv) Sunucu CPU'su yaklaşık 0,7 Hz örneklenmiştir; kısa tepeler kaçmış olabilir. (v) Bazı mekanizmalar (ikinci dalga, kayıp, sabit kuralın faz etkisi) doğrudan gözlenmemiş, hesapla ya da dolaylı kanıtla tutarlı bulunmuştur.

**Yapı geçerliliği.** Canlı alarm gecikmesi saniyelik p99 ile temsil edilmiştir; "alarm" ve "vital" ayrımı ve mesaj sıklığı varsayımsaldır. 100 ms eşiği klinik bir eşik değildir, yalnızca taban düzeyine göre bir referanstır. Gecikme, cihazın planlanan gönderim anından ölçülür (açık döngü), yani cihaz tarafı kuyruklanması da dahildir.

**Dış geçerlilik.** Tek makine, sanal cihazlar, sıfır gecikmeli ağ, iki kesinti türü ve üç kesinti süresi (sessiz kesinti ve süre taramaları yalnızca B1, N = 5.000, Mosquitto, TLS'siz için yapıldı), TLS sertifikası doğrulanmadan, yalnızca iki sunucu ve ikisi de tek düğüm. Sonuçlar mutlak kapasite iddiası taşımaz; yönlerin (sıralamanın) başka ortamlara taşınıp taşınmayacağı sınanmamıştır.

### 6.5 Sınırlar (ayrıntı)

**Bağlanma aşaması deneylerine özgü sınırlar (Bölüm 5.1-5.3).** (i) Bu bölümlerde kesinti süresi tek (60 sn); üstel kuralın TTR'si kesinti süresine bağlı testere dişi bir fonksiyon olduğundan TTR sonuçları bu süreye özgüdür. Kesinti süresi taraması yalnızca B1, N = 5.000, Mosquitto, TLS'siz için yapılmıştır (Bölüm 5.12) ve TTR o taramada raporlanmamıştır. (ii) Yalnızca iki cihaz sayısı (2.000, 5.000) denenmiştir. (iii) Sunucu açıkken görülen başarısız bağlantıların ve sabit kuraldaki faz etkisinin mekanizması doğrudan incelenmemiştir. (iv) Simülatörün CPU tepesi sınırın yaklaşık %61'ine çıkmıştır. (v) Koşular sırasında Docker'da başka bir projeye ait iki boşta konteyner çalışmaktaydı; etkisi ölçülmemiştir.

**Bağlantı sonrası yük deneylerine özgü sınırlar.** (i) Mosquitto tek iş parçacıklıdır; bulgular çok çekirdekli bir sunucuya genellenemez. (ii) Toplayıcı tek istemcidir ve onun yavaşlaması gecikmeye karışmış olabilir; toplayıcının işlem yükü ölçülmemiştir. (iii) N = 10.000'de taban gecikmenin yükselmesi simülatörün yükünü düşündürür; doğrulanmamıştır. (iv) Bölüm 5.5-5.7'deki 39 koşunun 6'sında 1 ile 132 arasında mesaj, toplayıcıya ulaşmamıştır (toplam yaklaşık 540 bin ile 1,6 milyon mesaj içinde, yani en çok yaklaşık %0,01); Bölüm 5.12'deki 24 koşunun 4'ünde 1, 2, 255 ve 314 mesaj ulaşmamıştır (en çok yaklaşık %0,07). Ham kayıt açıkken yapılan 12 ek koşuda (iki hücre, 6'şar tekrar) kayıp görülmemiş, olay yeniden üretilememiştir; nedeni açıklanamamıştır. Olası ama doğrulanmamış açıklamalardan biri, sunucunun açıldığı an ile toplayıcının yeniden abone olduğu an arasındaki yaklaşık 0,4-0,6 sn'lik pencerede cihazların gönderdiği mesajların abonesiz kalmasıdır (ölçülen yalnızca pencerenin varlığıdır); diğeri, durdurma anında sunucu belleğindeki iletilmemiş mesajların kaybıdır. Kaybın ham kayıt tarafından bastırılmış olması da dışlanamamıştır (günlük `09`). (v) B2 yalnızca iki ek ayarla sınanmış, ayar uzayı taranmamıştır. (vi) B3 dışındaki politikalarda biriken vital ve alarm mesajları aynı sırayla boşaltılır; B3'te öncelik yalnızca iki sınıf (alarm, vital) arasındadır. (vii) İş yükü (mesaj boyutu, vital ve alarm sıklığı, eşitleme boyutu) varsayımsaldır, klinik gerçekliği temsil etmez. (viii) Cihaz tamponu sınırsızdır ve tekrar önleme (dizi numarası, toplu onay) ya da sınırlı tampon için öncelikli saklama yoktur; bunlar [8]'de vardır. (ix) Cihazlar temiz başlangıçla (`CleanStart`) bağlanır; MQTT 5'in kalıcı oturum, sunucu tarafı kuyruk, `Receive Maximum` ve `Message Expiry Interval` özellikleri [9] denenmemiştir. (x) Sunucu tarafı hız sınırı denenmemiştir; [6]'nın yaklaşımı sunucu tarafıdır, bizimki cihaz tarafıdır. (xi) Rastgelelik penceresinin genişliği ve tavanı taranmamıştır; yalnızca tek ayar vardır [3]. (xii) Mesaj boyutu sabittir (128 bayt vital ve alarm, 4 KB eşitleme); [7]'deki gibi parça boyutu ile zamanlamanın birlikte ayarlanması denenmemiştir. (xiii) Web gezginindeki model elle kalibredir, 60 sn dışı kesinti sürelerinde güvenilir değildir ve [6]'daki gibi geçmiş olaylarla doğrulanmamıştır.

## 7. Sonuç ve Gelecek Çalışma

Bu çalışma, çok cihazlı bir MQTT 5 sisteminde sunucu kesintisinden sonra toplu yeniden bağlanmayı ve cihazların bağlandıktan sonra yaptığı işi (durum eşitleme ve biriken mesajların boşaltılması) canlı alarm gecikmesi açısından, iki sunucuda, tekrarlı ve kayıtlı deneylerle ölçmüştür. Başlıca sonuçlar: (i) bağlantı sonrası iş, canlı alarm gecikmesini bağlanmanın kendisinden çok daha fazla artırabilir (Mosquitto, TLS'siz, 5.000 cihazda yaklaşık 30 ms'den yaklaşık 9 sn); (ii) dönüşü rastgele dağıtmak sunucu zorlandığında (TLS, çok sayıda cihaz) toparlanmayı ve gecikmeyi belirgin biçimde düzeltir, TLS'siz ve rahat ortamda ise yalnızca bedel getirir; (iii) boşaltmayı zamana yaymak (B2), uygun ayarla doymuş ortamlarda gecikmeyi yaklaşık %89-99 düşürür; ancak ayar yüke göre büyütülmelidir (N = 10.000'de varsayılan 30 sn / 200 ms üstelde yalnızca %53 düşürmüş, rastgelelide düşürmemiştir) ve bedeli boşaltmanın ve birikmiş verinin uzamasıdır; (iv) bu sıralama çok çekirdekli EMQX'te de geçerlidir, çekirdek eklemek tek başına çözüm değildir; (v) en iyi politika ortama göre değişir. (vi) Alarmları öne almak (B3), rastgeleli kuralda birikmiş alarmın yaşını B1 düzeyine (medyan yaklaşık 37 sn) indirir ve canlı alarmı B2 kadar korur; ancak üstel dönüş ve TLS'te canlı alarmı B2'ye göre 2,3 kat kötüleştirir. (vii) Sessiz kesintide (ağdan kopma; yalnızca B1, N = 5.000) cihazlar kopmayı yaklaşık 11 sn geç ama eşzamanlı fark eder, izdiham dağılmaz; rastgeleli kuralda tepe 66 ms'den 638 ms'ye çıkar; yayın onayı zaman aşımı olmayan bir istemci sessiz kesintiyi hiç fark etmeyebilir (Bölüm 5.11). (viii) Kesinti süresi uzadıkça (15, 60, 180 sn) üstel kuralda tepe neredeyse doğrusal artar (1,9; 9,1; 20,0 sn), rastgeleli kuralın güvenli bölgesi daralır (49 ms'den 1.529 ms'ye, 180 sn'de) ve web gezginindeki basit model 60 sn dışı sürelerde güvenilir değildir (Bölüm 5.12). Bu sonuçlar bir ölçüm çalışmasının bulgularıdır: yeni bir algoritma önerilmemiştir, B3, [8]'deki öncelikli geri doldurma fikrinin bir uyarlamasıdır ve her bulgu bu tek makineli, sanal cihazlı düzeneğe göre yorumlanmalıdır.

Gelecek çalışma adayları, bu çalışmanın sınırlarından (Bölüm 6.5) çıkar: (i) rastgelelik penceresinin genişliğini ve tavanını tarayan bir deney [3]; (ii) sunucu tarafı hız sınırı ile cihaz tarafı boşaltma politikasının karşılaştırılması ve birlikte kullanımı [6]; (iii) kalıcı oturumun sunucu tarafı kuyruğu ve `Receive Maximum` ile akış denetiminin istemci tarafı politikalarla karşılaştırılması [9]; (iv) sınırlı cihaz tamponu ve öncelikli saklama, tekrar önleme [8]; (v) kesinti süresi taramasının B2, B3, TLS ve EMQX ile ve sessiz kesintiyle yinelenmesi; (vi) parça boyutu ile zamanlamanın birlikte ayarlanması [7]; (vii) sunucunun "ne kadar sonra gel" bilgisi vermesinin etkisi [2]; (viii) kayıp mekanizmasının ham kayıtla incelenmesi; (ix) modelin 15 ve 180 sn gibi yeni koşullarla yeniden kalibre edilmesi ve ayrı bir doğrulama kümesiyle sınanması; (x) kenar geçidi katmanı, toplayıcının da TLS'li olması ve gerçek donanım ya da gerçek ağ koşulları.

## Kaynakça

Bkz. `kaynakca.md`. Metinde geçenler:

[1] I. Fette ve A. Melnikov, "The WebSocket Protocol," RFC 6455, IETF, Aralık 2011.

[2] C. Jennings, Ed., R. Mahy, Ed. ve F. Audet, Ed., "Managing Client-Initiated Connections in the Session Initiation Protocol (SIP)," RFC 5626, IETF, Ekim 2009.

[3] S. Floyd ve V. Jacobson, "The Synchronization of Periodic Routing Messages," IEEE/ACM Transactions on Networking, c. 2, s. 2, s. 122-136, Nisan 1994.

[4] L. Huang, M. Magnusson, A. B. Muralikrishna, S. Estyak, R. Isaacs, A. Aghayev, T. Zhu ve A. Charapko, "Metastable Failures in the Wild," 16th USENIX Symposium on Operating Systems Design and Implementation (OSDI 22), Carlsbad, s. 73-90, Temmuz 2022.

[5] R. Isaacs, P. Alvaro, R. Majumdar, K.-K. Muniswamy-Reddy, M. Salamati ve S. Soudjani, "Analyzing Metastable Failures," Workshop on Hot Topics in Operating Systems (HotOS '25), Banff, Mayıs 2025, doi:10.1145/3713082.3730380.

[6] H. Xie, J. Liu, C. Yang, Y. He, L. Chen, D. Li, M. Fu, X. Chen ve S. Chen, "Open the Floodgates in a Digital Twin: Experiences of Building Spillway for 100M+-User Signaling Storms in Cellular Core Networks," ACM SIGCOMM 2026 Conference, s. 1779-1797, doi:10.1145/3789240.3829205.

[7] M. Domingues, J. N. Faria ve D. Portugal, "Dimensioning payload size for fast retransmission of MQTT packets in the wake of network disconnections," EURASIP Journal on Wireless Communications and Networking, 2024:2, doi:10.1186/s13638-023-02327-3.

[8] H. Bi, Y. Zhang, J. Jiang ve T. Guan, "Resilient End-Edge-Cloud Collaboration for Control Continuity and Closed-Loop Alarm Management in Solar Greenhouse IoT Systems Under Degraded Network Conditions," Applied Sciences, c. 16, s. 11, 5191, doi:10.3390/app16115191, Mayıs 2026.

[9] *MQTT Version 5.0*, OASIS Standard, 7 Mart 2019.

## Ekler

- EK-A: Deney kayıt defteri (`results/deney_kayitlari.csv`)
- EK-B: Koşu adları ve girdi dosyaları
- EK-C: Kod deposu bağlantısı (github.com/mertbayir/mqtt-analysis-project)
