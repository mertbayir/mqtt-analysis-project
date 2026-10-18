# İlgili çalışmalar: ne yapıyorlar, neyi yapmıyorlar, bizden farkı

Tarih: 18 Ekim 2026. Dayanak: `kaynaklar/` altındaki kopyalar.

**Ne kadar okudum (dürüstçe):**
- Domingues ve ark.: giriş, ilgili çalışmalar, yöntem, sonuçlar ve sonucu **baştan sona okudum**.
- Spillway: özet, kullanıcı/NE parametre tablosu (Tablo 7), CN-DES soyutlaması ve jitter ile ilgili bölümleri okudum; kalan kısmı **yalnızca terim taramasıyla** gördüm (mqtt, websocket, backlog, alarm vb. geçiyor mu).
- Huang, Isaacs, Floyd-Jacobson: özet, giriş, ilgili bölümler ve terim taraması. **Baştan sona okunmadılar.**
- MQTT 5.0: neden kodları, özellik tablosu ve ilgili cümleler.
- RFC 6455 §7.2.3 ve RFC 5626 §4.5: bölüm metinlerini okudum.

Aşağıdaki "geçmiyor" ifadeleri, çıkarılmış metinde terim sayımına dayanır (PDF metin çıkarımı eksik olabilir).

## 1. Domingues, Faria, Portugal (EURASIP JWCN 2024): en yakın MQTT çalışması

**Ne yapıyor.** Bağlantı koptuğunda yerel veritabanında biriken sensör verisini, bağlantı dönünce MQTT ile sunucuya geri yollayan bir mekanizma. Sunucu, istemci bağlanır bağlanmaz `SYNC_REQ` konusuna bir istek yayınlar; istemci veriyi parçalara bölüp `SYNC_REP` konusuna yollar ve sonunda `SYNC_REP_END` gönderir. Soru: **tek mesajın en iyi yük boyutu ne?** Mosquitto + Paho, Raspberry Pi istemci, ev Wi-Fi'si, kesinti 30 dk-24 sa, 4.300 test (hücre başına 100 tekrar), TLS 1.2. Ölçütler: yeniden iletim süresi ve verim. Sonuç: en iyi yük ≈ 13 MB (yaklaşık 15 dakikalık veri), kesinti süresinden bağımsız.

**Bizim için en önemli üç şey:**
1. **"Gelecek çalışma" cümlesi doğrulandı** (Sonuç bölümünün son paragrafı, s. 16-17): ek MQTT istemcileriyle denemek ve **aynı anda yeniden iletim yapan istemci sayısının etkisini** incelemek gelecek çalışma olarak bırakılmış. Biz tam bunu ölçüyoruz (1.000-10.000 cihaz).
2. **Canlı veri yönü ters.** Yazarlar, canlı veri aynı anda gönderildiğinde bunun *yeniden iletimi* etkilemediğini bulmuş (canlı akış 175 kbps, yeniden iletim 8,32 Mbps; s. 16). Biz tersini ölçüyoruz: boşaltmanın *canlı alarm gecikmesine* etkisi. Onların ölçütü canlı mesajın gecikmesi değil.
3. **Tek istemcide bile küçük mesaj sunucuyu kuyruğa sokmuş:** en küçük yük boyutunda (14,84 KB) saniyede yaklaşık 300 mesaj sunucuyu boğmuş ve kuyruk birikmiş (s. 14). Bu, bizim "çok sayıda küçük iş sunucu kuyruğunu doldurur" mekanizmamızla uyumlu bir küçük ölçekli gözlem. Mekanizma olarak söylemeyelim, "uyumlu" diyelim.

**Dikkat (makaleye yazılmalı).** Bizim cihazdaki "eşitleme isteği → yanıt → boşaltma" akışı, Domingues'in `SYNC_REQ/SYNC_REP` akışına **tasarım olarak benziyor**. Yöntem bölümünde bu benzerlik açıkça söylenmeli ve farkımız yazılmalı (bizde istek cihaz tarafında başlıyor ve boşaltma politikası B0-B3 ile değişiyor; onlarda sunucu istiyor ve tek politika var, parça boyutu değişiyor).

**Çerçeve uyumu.** Çalışma "dijital sağlık" senaryosunda (WoW projesi, hasta yatağında akıllı kutu). Bizim tıbbi çerçevemiz için doğal dayanak.

**Sınırları (kendi sözleriyle ve bizim gözlemimiz).** Tek istemci. Yük boyutu MB mertebesinde; biz tek mesaj boyutunu sabit tutup *zamanlamayı* (B2 gecikme ve aralık) ve *sırayı* (B3 alarm önce) değiştiriyoruz. Canlı gecikme ölçülmemiş. Yeniden bağlanma kuralı (geri çekilme) konu değil.

## 2. Spillway (Xie ve ark., SIGCOMM 2026): kavramda en yakın, katmanda uzak

**Ne yapıyor.** Hücresel çekirdek ağda (5G Standalone) milyonlarca cihazın eşzamanlı yeniden bağlanma denemelerinden doğan "sinyalleşme fırtınalarını" azaltıyor. Parçalar: (i) dijital ikiz, (ii) CN-DES adlı ayrık olay simülatörü; aynı protokol durumundaki kullanıcıları vektörlerde toplayarak maliyeti kullanıcı sayısından ayırıyor (geleneksel simülasyona göre 60× hız), (iii) HEBO (Bayesçi optimizasyon) ile parametre araması, (iv) hiyerarşik savunma: üst düğümler yükü erken atıyor ("fedakâr kısıtlama"). Beş yıllık gerçek dağıtım; gerçek olaylarda tahmin edilen eski ayara göre çok daha az kullanıcı geri düşmesi (fallback).

**Ayarladığı şey sunucu (ağ) tarafı.** Tablo 7: AMF/SMF/UDM üzerinde token kovası kapasitesi, doldurma hızı, aşırı yük eşiği, oturum hız sınırı, PDU kuyruk derinliği "kritik" (optimize ediliyor). Geri çekilme taban süresi, jitter aralığı (%50-95), en çok deneme sayısı **kritik değil** (satıcı varsayılanlarında sabit).

**Bizim için önemli iki ayrıntı:**
1. **Jitter ile ilgili iddiası.** Büyük ölçekte bireysel zamanlayıcı jitter'larının ortalamaya gittiğini, makroskopik fırtına dinamiğini belirleyenin trafik stokastikliği olduğunu söylüyorlar (CN-DES bölümü). Ama modelde **jitter dağılımının yayma etkisini koruyorlar** (kullanıcı vektörünü jitter dağılımına göre çocuk vektörlere bölüyorlar). Bu bizim bulgumuzla **çelişmiyor**: biz bekleme *kuralının* (üstel mi rastgeleli mi) sonucu kökten değiştirdiğini gösteriyoruz (N = 5.000, 60 sn: tepe 9.066 ms'ye karşı 49 ms), yani rastgeleliğin düzeyi/biçimi kritik. Aynı şeyi ölçmüyoruz; çelişki iddiası yazılmamalı.
2. **Yöntem benzerliği.** Kullanıcıları durumda toplayan model fikri bizim web modelimize benziyor (biz akışkan kuyruk + durum makinesi). Modelimiz makaleye girmiyor (sunum aracı), ama Tartışma'da "benzer bir soyutlama kullanılmış" denebilir.

**Neyi yapmıyor (terim taraması, çıkarılmış metinde):** MQTT, WebSocket, IoT, kalıcı bağlantı geçmiyor; birikmiş veri (backlog, buffered, flush) ve alarm geçmiyor. Yani **bağlantı sonrası iş ve canlı mesaj gecikmesi ölçülmüyor**; ölçütleri kullanıcı geri düşmesi ve toparlanma süresi. Katman farklı (3GPP sinyalleşme, ağ elemanı hız sınırı), kontrol edilen taraf farklı (sunucu), sistem farklı (operatör ağı, gerçek dağıtım).

**Sonuç.** "Cihaz kümesinin eşzamanlı yeniden bağlanması metastabil aşırı yük yaratır" fikri yayımlanmış ve bizden yeni değil. Bizim farkımız: uygulama katmanı kalıcı bağlantı (MQTT 5), **cihaz tarafı** bekleme kuralı ve bağlantı sonrası boşaltma politikası, canlı alarm gecikmesi ölçütü, açık ve tekrarlanabilir ölçüm. Giriş'te bu fark Spillway'e açıkça bağlanmalı. Önceki "kısmi-yüksek çakışma" tahmini (özetten) tam metne göre **kavramda yüksek, kapsamda düşük** diye düzeltilebilir, ancak ben tam metni baştan sona okumadım.

## 3. Huang ve ark. (OSDI 2022): metastabil çöküş, yeniden deneme ana sürdürücü

22 metastabil çöküşü 11 kuruluştan inceliyor. Tetikleyici, sürdürücü etki ve yükseltme döngüsü çerçevesi. **En yaygın sürdürücü etki yeniden deneme politikası** (olayların yarısından fazlası; listelenen 11 olay). Çözüm odağı: yük atma, yük azaltma.
- **İstek/cevap servisleri** modelleniyor (istek başına kaynak maliyeti). Metinde reconnect, jitter, backoff, MQTT, WebSocket, session geçmiyor (sayım: 0).
- **Bizim için:** yeniden denemenin ve eşzamanlılığın çöküşü sürdürdüğü fikri bizim "ikinci dalga" (TLS'te t ≈ 60 sn) bulgumuzla uyumlu. Bunu "metastabil" diye adlandırmak için ölçümümüz yok; Tartışma'da "uyumlu olabilir" denir.

## 4. Isaacs ve ark. (HotOS 2025): analiz yöntemi

Kuyruk teorisi modeli + ayrık olay simülasyonu + emülatör ile hangi koşullarda metastabil çöküş olacağını önceden bulma. Yine istek/cevap servisi ve yeniden deneme; reconnect, jitter, backoff, MQTT geçmiyor.
- **Bizim için:** yöntem akrabalığı. Bizim web modelimiz "basit model + ölçümle sınama" fikrinin küçük bir örneği. **Adım 10'daki kesinti süresi testi tam olarak bu sınav:** model 15 sn'de 4,8 kat yanıldı. Bu, makalenin Tartışma'sında modelin sınırı olarak yazılabilir.

## 5. Floyd ve Jacobson (1994): rastgelelik neden gerekli

Bağımsız görünen periyodik işlemlerin istemeden eşzamanlı hâle geldiğini, geçişin **ani bir faz geçişi** olduğunu ve eşzamanlılığı önlemek için eklenmesi gereken rastgeleliğin **şaşırtıcı derecede büyük** olduğunu gösteriyor (örnek: 300 ms işleme süresi için en az yaklaşık 1 sn).
- **Bizim için:** rastgele gecikme eklemenin teorik dayanağı. Adım 10'da "rastgeleli kuralın güvenli bölgesi 180 sn kesintide daralıyor" bulgusu, rastgelelik penceresinin yüke göre büyümesi gerektiğiyle **tutarlı olabilir**; bu bağlantıyı biz sınamadık.
- Konu yönlendirme mesajları (periyodik). Yeniden bağlanma değil; benzetme olarak anılır, doğrudan kanıt olarak değil.

## 6. MQTT 5.0 standardı: doğrulananlar

- Neden kodları var: `0x89 Server busy` ("sonra tekrar dene" anlamında), `0x9F Connection rate exceeded`, `0x9C Use another server`, `0x9D Server moved` (+ `Server Reference` ile yönlendirme), `Server Keep Alive`.
- **"Ne kadar sonra gel" diyen bir alan yok.** Özellik tablosunda (CONNACK/DISCONNECT) bekleme süresi veren bir kimlik yok; metinde geri çekilme (back-off) ve jitter geçmiyor (sayım: 0). "Retry" yalnızca ileti teslimi yeniden denemesi (§4.4) için.
- Oturum ve ileti ömrü araçları var: `Session Expiry Interval`, `Message Expiry Interval`. İleti son kullanma süresi, biriken mesajı atma (B0'a benzer bir fikir) için standartta karşılığı olan bir mekanizma. Ama sunucu kuyruğu için; bizim birikim cihaz tarafında. Tartışma'da "ilgili standart araç" diye anılabilir.

## 7. RFC'ler: doğrulananlar ve **düzeltilecek bir nokta**

- **RFC 6455 §7.2.3:** ilk yeniden bağlanma rastgele geciktirilmeli (0-5 sn "makul"), sonrakiler kesilmiş ikili üstel geri çekilmeyle uzamalı. Okundu, doğrulandı.
- **RFC 5626 §4.5 ("Flow Recovery"):** `W = min(max-time, base-time × 2^ardışık-hata)`; gerçek bekleme W'nin %50-100'ü arası düzgün rastgele. Okundu, doğrulandı. **Varsayılanlar: taban 30 sn (hepsi başarısızsa) veya 90 sn, tavan 1800 sn.**
- **Bizim "rastgeleli üstel" kuralımız:** ilk bekleme U(0, 5 sn) (RFC 6455 fikri), sonra U(0,5 W, W) (RFC 5626'daki %50-100 fikri); ama **taban 1 sn, tavan 30 sn**. Yani şekli aldık, RFC 5626'nın sabitlerini almadık (onlarla bekleme dakikalar-saatler olurdu). **Makalede bunu açıkça yazmak gerekiyor**; şu an Giriş yalnızca formülü veriyor, sabitlerin farkını söylemiyor.
- Künye düzeltmesi: RFC 5626'nın Jennings, Mahy, Audet adları RFC üstbilgisinde **editör (Ed.)** olarak geçiyor.

## 8. Ne değişiyor: yapılacaklar

1. `makale/kaynakca.md`: Domingues, Spillway, Huang, Isaacs, Floyd-Jacobson, MQTT 5.0 ve iki RFC "doğrulandı"ya taşınır (künye `kaynaklar/INDEX.md`'de); RFC 5626 editör notu.
2. `makale/taslak.md`: "İlgili Çalışmalar" bölümü yazılır (şimdi `[ ]` yer tutucular var). Giriş'te Spillway ve Domingues'e açık fark cümlesi. Yöntem'de RFC sabitleri farkı ve `SYNC_REQ` benzerliği.
3. **B3 yenilik cümlesi** ("bildiğimiz kadarıyla benzer değerlendirme bulunamadı"): okuduğum altı çalışmada alarm-önce boşaltma yok, ama **Bi ve ark. 2026 (önce-öncelik, oran sınırlı doldurma)** ve PrioMQTT okunmadı. Önce onlar okunmadan cümle güçlendirilmemeli.
4. `15b`/`15c` içindeki "ajan okudu" ifadeleri: Domingues "gelecek çalışma" cümlesi **şimdi bizim gözümüzle doğrulandı**; Spillway "okunamadı" idi, **şimdi okundu (kısmen)**.
