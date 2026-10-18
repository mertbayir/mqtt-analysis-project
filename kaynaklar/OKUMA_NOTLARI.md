# Okuma notları: ilgili çalışmalar, bizim konumumuz, atıf hazırlığı

Tarih: 18 Ekim 2026. Bu dosya `ANALIZ.md`'nin yerine geçer (onun ilk taslağındaki üç hata düzeltildi: Spillway'in geri çekilme parametreleriyle ilgili ifadesi, Huang'daki olay sayısı ve Bi ve ark.'nın durumu).

**Nasıl okunur.** Her kaynak için aynı sekiz başlık var: (1) künye ve erişim, (2) ne söylüyor, (3) ne ölçmüş ve hangi ortamda, (4) bizdeki artılar, (5) bizdeki eksiler, (6) farkımız / neyi iyileştiriyoruz, (7) makalede atıf cümlesi taslağı, (8) PDF'te doğrulama işaretçisi ve uyarılar.

**Atıf anahtarları** (geçici; makaleye numara verilince değişecek): `[RFC6455]` `[RFC5626]` `[Domingues24]` `[Xie26]` `[Huang22]` `[Isaacs25]` `[Floyd94]` `[MQTT5]` `[Bi26]` `[STGen26]`.

**Alıntı kuralı (kendimize).** Metne mümkünse **parafraz** koyarız. Doğrudan alıntı gerekirse en fazla bir cümle, tırnak içinde, sayfa numarasıyla ve PDF'ten kopyalanarak yazılır; bu dosyada alıntı yerine *işaretçi* (bölüm, sayfa, PDF'te aranacak anahtar sözcük) verilir ki cümleyi kaynağından kendiniz alın. Sayfa numaraları PDF'teki sayfa işaretlerinden çıkarıldı; Spillway ve Bi ve ark. için ±1 sayfa sapma olabilir, bu yüzden **bölüm numarası** esas alınır.

**Okuma derinliği** (dürüst): Domingues, Huang, Isaacs ve Spillway'in ana metni **baştan sona**; Floyd-Jacobson'ın giriş, model, rastgelelik rehberi ve sonuç bölümleri (Markov zinciri analizinin ayrıntıları atlandı); Bi ve ark.'nın özet, yöntem, geri doldurma, sonuç, ablasyon ve sınırlılık bölümleri; STGen'in özet, dağıtık deney ve geçerlilik sınırları; MQTT 5.0'ın ilgili bölümleri ve terim taraması; iki RFC'nin ilgili bölümleri. Okunmayanlar sonda.

---

## 0. Tek bakışta karşılaştırma

| Boyut | Biz | Domingues24 | Xie26 (Spillway) | Bi26 | Huang22 / Isaacs25 | Floyd94 |
|---|---|---|---|---|---|---|
| Sistem | MQTT 5, uygulama katmanı | MQTT, uygulama katmanı | 5G çekirdek sinyalleşmesi | Sera IoT, uç-bulut | İstek/cevap servisleri | Yönlendirici mesajları |
| Eşzamanlı cihaz | 500-10.000 | **1** | milyonlar (model); gerçek olaylarda 3,4-20 milyon | tek geçit | tek/birkaç istemci | 20'ye kadar yönlendirici |
| Gerçek sunucu | Mosquitto, EMQX | Mosquitto | Gerçek çekirdek ağ (5 yıl) + simülatör | Yok (Python simülasyonu) | Gerçek DB / emülatör | Simülasyon |
| Cihaz tarafı yeniden bağlanma kuralı | **Karşılaştırıldı** (3 kural) | Konu değil | Sabit varsayım (Tablo 2 örneği) | Konu değil | Yeniden deneme sayısı/zaman aşımı | Rastgelelik miktarı |
| Bağlantı sonrası birikmiş boşaltma | **Ölçüldü** (B0-B3) | Ölçüldü (tek istemci, parça boyutu) | Yok | Önerildi (simülasyon) | Yok | Yok |
| Canlı mesaj gecikmesi | **Ana ölçüt** | Ölçüt değil | Ölçüt değil (geri düşen kullanıcı) | Ölçüt değil | İstek gecikmesi (farklı anlam) | Yok |
| Tekrar sayısı | 3-4 | 100 | 50 senaryo (simülatör) | 20 | birkaç | Simülasyon |
| Kayıt/tekrarlanabilirlik | Tüm koşular kayıtlı, hipotezler önceden | Betikler var | Ekler hakemsiz, veri özel | Betik ve veri ekte | Örnek uygulamalar açık | Eski |

---

## 1. Domingues, Faria, Portugal (EURASIP JWCN 2024): `[Domingues24]`

**(1) Künye.** M. Domingues, J. N. Faria, D. Portugal, "Dimensioning payload size for fast retransmission of MQTT packets in the wake of network disconnections," *EURASIP J. Wireless Commun. Netw.* 2024:2, doi:10.1186/s13638-023-02327-3. CC BY 4.0. Yerel: `domingues_2024_eurasip_payload.pdf`. **Tam okundu.**

**(2) Ne söylüyor.**
- Bağlantı kopunca sensör düğümü veriyi yerel veritabanında (MongoDB) tutar; bağlantı dönünce **sunucu**, `SYNC_REQ` konusuna bir istek yayınlar, istemci veriyi parçalara bölüp `SYNC_REP` konusuna yollar, bitince `SYNC_REP_END` gönderir (§3, s. 8-9).
- Ana soru: tek mesajın **en uygun yük boyutu**. Ortada bir tatlı nokta bulunmuş: yaklaşık 13 MB (≈ 15 dakikalık veri) en kısa yeniden iletim süresini ve en yüksek verimi vermiş; kesinti süresinden bağımsız (§5, s. 15; §6, s. 16).
- Çok küçük parçalar kötü: 14,84 KB'lık yükte saniyede yaklaşık 300 mesaj sunucuyu doyurmuş ve kuyruk birikmiş (§5, s. 14).
- Biriken veri boşaltılırken canlı veri de akıtılınca yeniden iletim süresi neredeyse değişmemiş; çünkü canlı akış (175 kbps) boşaltma akışının (8,32 Mbps) çok altında (§5, s. 16, Şekil 6).
- Yazarlar, kopma sonrası biriken verinin hızlı kurtarılmasını MQTT için açıkça ele alan bir çalışma bulamadıklarını söylüyor (§2 sonu, s. 7).
- **Gelecek çalışma:** ek MQTT istemcileriyle denemek ve **aynı anda yeniden iletim yapan istemci sayısının** etkisini incelemek (§6 sonu, s. 16-17).

**(3) Ne ölçmüş.** Tek istemci (Raspberry Pi 4B), sunucu Intel NUC üzerinde Mosquitto, istemci Paho, ev Wi-Fi'si, TLS 1.2; kesinti 30 dk-24 sa; 4.300 test, hücre başına 100 tekrar. Ölçütler: **yeniden iletim süresi** ve **verim** (§4-5, s. 10-13). Dijital sağlık örneği (WoW projesi, hasta yatağı akıllı kutusu).

**(4) Bizdeki artılar.**
- **Çok istemci** (500-10.000 cihaz): onların açıkça bıraktığı soru.
- **Canlı mesajın gecikmesi** ana ölçütümüz (alarm p99). Onlarda yön ters: canlı akışın boşaltmaya etkisi.
- **Boşaltma politikası** boyutunda zamanlama ve sıra (B1: hepsi hemen, B2: zamana yay, B3: alarm önce); onlarda yalnızca parça boyutu.
- **Bekleme kuralı** (sabit/üstel/rastgeleli) karşılaştırması: onlarda konu dışı.
- İki sunucu (Mosquitto, EMQX), TLS'li ve TLS'siz.
- Hipotezlerin önceden yazılması, tüm koşuların girdi ve çıktılarıyla kayıtlı olması.

**(5) Bizdeki eksiler.**
- Onlar **gerçek donanım** (Raspberry Pi, Wi-Fi, gerçek sensör verisi) kullanmış; biz tek makinede sanal cihaz.
- Hücre başına **100 tekrar**, biz 3-4.
- Onlar **MB mertebesinde** yük ve saatlerce kesinti denemiş (24 sa); biz 128 bayt mesaj, 4 KB eşitleme, 15-180 sn kesinti.
- Biz **parça boyutu** boyutunu taramadık (B2'de yalnızca zamanlama; mesaj boyutu sabit).
- Onların mimarisi sunucu tetikli (`SYNC_REQ`); bizimki cihaz tetikli. İkisi de eşitleme isteği içerdiği için **tasarım olarak benziyor** (aşağıya bak).

**(6) Farkımız.** Domingues24 tek istemcinin biriken veriyi sunucuya ne kadar hızlı verdiğini ölçer ve çok istemcili durumu gelecek çalışmaya bırakır. Biz çok istemcili durumu, canlı alarm gecikmesi açısından ölçüyoruz.

**(7) Atıf cümlesi taslakları** (Bölüm 2 İlgili Çalışmalar, Bölüm 3 Yöntem, Bölüm 6 Tartışma):
- *İlgili Çalışmalar:* "Domingues ve ark. [Domingues24], kopma sonrası biriken verinin MQTT ile sunucuya geri yüklenmesini tek bir istemci için incelemiş ve tek bir mesajın en uygun yük boyutunun yaklaşık 13 MB olduğunu bulmuştur; çok sayıda istemcinin aynı anda geri yükleme yapmasının etkisini gelecek çalışmaya bırakmışlardır. Biz bu soruyu, canlı alarm gecikmesi ölçütüyle ve 10.000 cihaza kadar inceliyoruz."
- *Yöntem:* "Cihazın bağlandıktan sonra sunucudan yapılandırma ve durum eşitlemesi istemesi, [Domingues24]'deki `SYNC_REQ/SYNC_REP` akışına benzer; farkımız, isteğin cihaz tarafından başlatılması ve boşaltma zamanlamasının (B0-B3) politika olarak değiştirilmesidir."
- *Tartışma:* "Küçük mesajların çok sık gönderilmesinin tek istemcide bile Mosquitto'da kuyruk yarattığı bildirilmiştir [Domingues24]; bizim bulgumuz (çok sayıda cihazın birikmiş küçük mesajları aynı anda boşaltması) bununla uyumludur."

**(8) PDF işaretçileri ve uyarılar.**

| İddia | Yer | PDF'te ara |
|---|---|---|
| Sunucu tetikli `SYNC_REQ/REP` akışı | §3, s. 9 | "SYNC_REQ" |
| En iyi yük boyutu ≈ 13 MB | §5 s. 15, §6 s. 16 | "13.04 MB" |
| Küçük yükte ≈ 300 mesaj/sn sunucuyu doyurdu | §5, s. 14 | "300 messages per second" |
| Canlı veri yeniden iletimi etkilemedi | §5, s. 16 | "175.07 kbps" |
| Literatürde ilgili çalışma bulunamadı | §2 sonu, s. 7 | "could not find any work" |
| Gelecek çalışma: eşzamanlı istemci sayısı | §6, s. 16-17 | "simultaneous retransmission processes" |
| 4.300 test, 100 tekrar | §5, s. 12 | "4300" |

*Uyarılar.* (a) "Hiç kimse yapmamış" demeyin: yalnızca "tek istemci için incelenmiş, çok istemci gelecek çalışma" diyebiliriz. (b) Optimal yük boyutunu kendi sonucumuza uygulamayın, onlar bunu uygulamaya özgü olabileceğini kendileri söylüyor (§5, s. 15). (c) Sağlık çerçevemizi desteklese de bu çalışma da kendi senaryosunu "sağlık" olarak çerçevelemiş; biz yalnızca senaryo olarak kullanıyoruz.

---

## 2. Xie ve ark., Spillway (ACM SIGCOMM 2026): `[Xie26]`

**(1) Künye.** H. Xie, J. Liu, C. Yang, Y. He, L. Chen, D. Li, M. Fu, X. Chen, S. Chen, "Open the Floodgates in a Digital Twin: Experiences of Building Spillway for 100M+-User Signaling Storms in Cellular Core Network," *Proc. ACM SIGCOMM 2026*, s. 1779-1797, doi:10.1145/3789240.3829205. Tsinghua Üniversitesi, China Mobile, Zhongguancun Laboratuvarı. CC BY 4.0. Yerel: `xie_2026_sigcomm_spillway.pdf`. **Ana metin baştan sona okundu**; ekler ve tablolar taranmıştır.

**(2) Ne söylüyor.**
- **Problem (§1, §2.3, s. 1779-1781):** Bölgesel bir arıza milyonlarca cihazı aynı anda koparır; cihazlar yaklaşık aynı anda yeniden bağlanmaya çalışır. İç kuyruklar dolar, bir isteğin sistemde kalma süresi cihazın zaman aşımını (10-15 sn) geçer, ağ artık kimsenin dinlemediği yanıtlar için çalışır ("boşuna iş") ve cihazlar yeniden dener. Bu bir **histerezis** döngüsü: trafik normale dönse de sistem toparlanmaz; kuyruğu boşaltmak için trafiğin sıfıra yakın düşürülmesi gerekir.
- **Mevcut savunmalar (§2.4, s. 1781-1782):** CPU yüküne göre, kullanıcı kapasitesine göre ve bir sonraki döngüyü "ileri bakışla" ayarlayan üç yerel sınırlama yöntemi. Hepsi yerel; zincirdeki yukarı düğüm, aşağıdaki darboğazı görmediği için fazla trafiği içeri alır. Reddedilen cihazlara **geri çekilme kodlarıyla** yanıt veriliyor (UC tabanlı yöntemde).
- **Spillway'in fikri (§3-4, s. 1782-1783):** Yukarıdaki düğümler (erişim katmanı) kendi kapasitelerinin altında kalacak şekilde bilerek yük atar ("fedakâr kısıtlama"); sınırlar, standart ve satıcıdan bağımsız parametrelerle, **statik** olarak önceden kurulur. Çalışma anında etkin bir kontrol döngüsü yok.
- **CN-DES (§5, s. 1783-1786):** Aynı protokol durumundaki kullanıcıları vektörler hâlinde toplayan ayrık olay simülatörü; maliyet kullanıcı sayısından bağımsız (100 milyon kullanıcı, yaklaşık 1 GB bellek). Büyük sayılar yasasıyla bireysel zamanlayıcı jitter'ının büyük ölçekte ortalamaya gittiği savunuluyor (§5.2, s. 1785). **Ama** jitter'ın yayma etkisi modelde korunuyor: kullanıcı vektörü, jitter dağılımına göre zaman kovalarına bölünüyor (Ek B, s. 1792).
- **Doğrulama:** 1,16 milyon kullanıcıda ayrıntılı simülasyona göre %96,2 doğruluk (§7.1, s. 1786); 14 geçmiş olayda %91,7 (geri düşen kullanıcı) ve %88,8 (toparlanma süresi) doğruluk (§7.1 s. 1787; §8.3 s. 1789-1790'da MAPE %8,3 ve %11,2). Canlı testte tepe 120.000'den 35.000 istek/sn'ye indi (§7.3, s. 1788; §8.3).
- **Optimizasyon:** HEBO ile 42 kritik parametre; aşırı yük sınırında varyansın patladığı (heteroskedastisite) vurgulanıyor (§6, s. 1785-1786).
- **Sonuçlar:** Gerçek olaylarda (örn. 17,81 milyon cihazın uygulama hatasıyla eşzamanlı yeniden denemesi, 2024) önceden kurulan ayarlar sunucu aşırı yükünü önlemiş (§8.2, s. 1788-1789).
- **Dersler (§9, s. 1790):** Ders 4: 3GPP'de uçtan uca geri basınç (backpressure) protokolü yok, düğüm düğüm reddetme (HTTP 503 gibi) fırtınada yavaş. Ders 5: Var olan standart parametrelerin gizli kontrol kapasitesini kullanmak.

**(3) Ne ölçmüş.** Ölçütler: **geri düşen kullanıcı sayısı** (5G'den çıkıp 3G/4G'ye düşen) ve **toparlanma süresi**; zirve sinyalleşme hızı. Ayarladığı şey **ağ (sunucu) tarafı** parametreleri: jeton kovası kapasitesi ve dolum hızı, aşırı yük eşiği, oturum hız sınırı, kuyruk derinliği (Tablo 1, s. 1782; Tablo 7, s. 1795). Cihaz tarafı geri çekilme taban süresi, jitter aralığı (%50-95) ve en çok deneme sayısı Tablo 7'de **"kritik değil"** (satıcı varsayılanında sabit). **Not:** Metin kendi içinde tam tutarlı değil: §3.2'de (s. 1783) "T3410 zamanlayıcıları, geri çekilme aralıkları" ayarlanan standart parametreler arasında sayılıyor. Biz "cihaz tarafı kuralı ayarlamıyor" yerine "optimizasyonun kritik parametreleri sunucu tarafı; cihaz geri çekilme parametreleri Tablo 7'de sabit" diyeceğiz.

**(4) Bizdeki artılar.**
- **Katman:** uygulama katmanı kalıcı bağlantı (MQTT 5). Spillway'de MQTT, WebSocket, IoT ve kalıcı bağlantı hiç geçmiyor (terim taraması: 0).
- **Cihaz tarafı** bekleme kuralı ve bağlantı sonrası politika; Spillway sunucu tarafında kısıyor.
- **Canlı mesaj gecikmesi** ve **birikmiş veri boşaltması**; Spillway'de bunlar (birikmiş/tamponlanmış veri, alarm) geçmiyor.
- Açık, kayıtlı, **tekrarlanabilir** ölçüm; Spillway'de veri özel, ekler hakemsiz.
- Bekleme kuralının **etkisinin ölçümü**: üstel/rastgeleli farkı (örn. N = 5.000, 60 sn: 9.066 ms'ye karşı 49 ms).

**(5) Bizdeki eksiler.**
- **Ölçek ve gerçeklik:** 622 milyon abonelik gerçek ağda 5 yıl, 14 geçmiş olay ve canlı saha testiyle doğrulanmış; biz tek makinede sanal cihaz.
- **Doğrulanmış dijital ikiz:** Spillway'in simülatörü %88-92 doğrulukta; bizim modelimiz elle kalibre, 28/38 hücrede tutuyor ve kesinti süresi taramasında 6 hücrenin 4'ünde (üstel 15 sn'de 4,8 kat sapma).
- **Otomatik optimizasyon** (HEBO) ve **sunucu tarafı** hız sınırı yok; biz yalnızca cihaz tarafını denedik.
- Spillway'in "boşuna iş" (cihaz zaman aşımından sonra sunucunun işi sürdürmesi) kavramı bizim modelimizde `bayat` parametresi olarak var ama **doğrudan ölçmedik**.

**(6) Farkımız.** Spillway, hücresel çekirdekte sunucu tarafı hız sınırlarını optimize eder; biz MQTT'de cihaz tarafı bekleme kuralının ve bağlantı sonrası boşaltmanın canlı alarm gecikmesine etkisini ölçeriz. **Aynı problemin (eşzamanlı yeniden bağlanma → kuyruk → aşırı yük) farklı katmanda ve farklı kontrol noktasında ele alınışı.**

**(7) Atıf cümlesi taslakları.**
- *Giriş:* "Eşzamanlı yeniden bağlanmanın ardışık aşırı yük yarattığı, hücresel çekirdek ağlarında yaygın olarak raporlanmıştır; Xie ve ark. [Xie26], bunu sunucu tarafı hız sınırlarını sayısal ikiz ve Bayes optimizasyonuyla ayarlayarak azaltmayı ve beş yıllık bir dağıtımı raporlamıştır."
- *İlgili Çalışmalar:* "Bu çalışma 5G çekirdek sinyalleşmesine odaklanır ve ayarladığı parametreler ağ elemanlarının hız sınırlarıdır; MQTT veya WebSocket gibi uygulama katmanı kalıcı bağlantıları, cihaz tarafı bekleme kuralını ve bağlantı sonrası birikmiş veri boşaltmasını ele almaz. Bizim çalışmamız bu üç noktada farklıdır."
- *Tartışma (modelimizin sınırı):* "Bu çalışmada olduğu gibi [Xie26], benzer durumdaki cihazları toplayan bir soyutlama kullandık; ancak modelimiz elle kalibre edilmiştir ve 60 sn dışındaki kesinti sürelerinde güvenilir değildir (Bölüm 5.12)."
- *Tartışma (standartta boşluk):* "Xie ve ark. de uçtan uca geri basınç mekanizmasının standartta bulunmadığını bildirir [Xie26]; MQTT 5'te de bir bekleme süresi bildiren özellik yoktur [MQTT5]."

**(8) PDF işaretçileri ve uyarılar.**

| İddia | Yer | PDF'te ara |
|---|---|---|
| Eşzamanlı kurtarma olayı, boşuna iş, histerezis | §2.3 | "synchronized recovery events", "Futile Work", "hysteresis" |
| Üç yerel savunma ve "back-off codes" | §2.4 | "UC-based", "back-off codes" |
| Fedakâr kısıtlama | §4.1 | "altruistic" |
| Bireysel jitter ortalamaya gider | §5.2 | "individual-level jitters" |
| Jitter yayma etkisi korunuyor | Ek B | "Stochastic backoffs" |
| Tablo 7: kritik / değil parametreler | Ek D (Tablo 7) | "parameter taxonomy" |
| Doğruluk %96,2 / %91,7 / %88,8 | §7.1 | "96.2%", "91.7%" |
| 35.000'e karşı 120.000 istek/sn | §7.3, §8.3 | "35,000 requests" |
| Ders 4: geri basınç yok | §9 | "Lack of Standardized End-to-End Backpressure" |

*Uyarılar.* (a) Ekler **hakemsiz** (belirtilmiş: "Appendices ... not been peer-reviewed"); ekten alınan sayıyı ana metin gibi sunmayın. (b) "Cihaz tarafı kuralı ayarlamıyor" cümlesini doğrudan yazmayın (yukarıdaki tutarsızlık). (c) Bizim bulgumuzla "çelişir" demeyin: jitter'ın ortalamaya gitmesi onların ölçeğinde (milyonlarca bağımsız cihaz, rastgele varışlar) bir varsayım; bizde kesinti herkesi **aynı anda** başlatıyor ve kural, ortalama dinamiği değiştiriyor. Konu farklı. (d) "Çok daha büyük, bizimki önemsiz" tonundan kaçının, "kapsam farkı" deyin.

---

## 3. Huang ve ark., "Metastable Failures in the Wild" (OSDI 2022): `[Huang22]`

**(1) Künye.** L. Huang, M. Magnusson, A. B. Muralikrishna, S. Estyak, R. Isaacs, A. Aghayev, T. Zhu, A. Charapko, *16th USENIX OSDI*, Carlsbad, 11-13 Temmuz 2022, s. 73-90 (sayfa aralığı Isaacs25'in kaynakçasından). Açık erişim. Yerel: `huang_2022_osdi_metastable.pdf`. **Baştan sona okundu** (ekteki ispatlar hariç).

**(2) Ne söylüyor.**
- Halka açık olay raporlarından **21 metastabil çöküş** (Tablo 1, s. 75); özet ve girişte "22 metastabil çöküş, 11 kuruluş" deniyor (21 halka açık rapor ve §4'teki Twitter vakası olduğu anlaşılıyor; metin toplamı açıkça yazmıyor) (§1-2, s. 73-75).
- Çerçeve (§3, s. 76-80): **tetikleyici** (yük artışı ya da kapasite düşüşü), **sürdürücü etki** (tetikleyici kalktıktan sonra bile sistemi aşırı yükte tutan geri besleme) ve **yükseltme** (iş yükü yükseltmesi ya da kapasite bozulması). Aşırı yük tetikleyicisi yoksa metastabil çöküş olmaz (Teorem 1); yeterince düşük yükte sistem hep kararlıdır (Teorem 2).
- **Açık "kırılganlık dereceleri"** (Teorem 3, s. 79-80): kırılganlık ikili değil; yükseltme faktörleri tetikleyicinin **süresi** arttıkça artar, yani **daha uzun tetikleyici daha kırılgan** (s. 80).
- En yaygın sürdürücü etki **yeniden deneme politikası**: raporlanan olayların yarısından fazlasında (listelenen 11 olay; s. 75). Kurtarma çoğunlukla **yük atma** (>%50-55; s. 75, 80).
- Deneyler (§5, s. 82-85): MongoDB üç kopyalı kümede (6.200 istek/sn, 3 sn zaman aşımı, 4 yeniden deneme), tetikleyiciyi **10 sn** tutmak sistemi çökertirken **9 sn** tutmak kurtarmıştır (Şekil 5, s. 83): küçük bir süre farkı kritik olabilir.
- **"Düzeltme bozar"** (§6.3, s. 85): sınırsız yeniden deneme, olayın ardından getirilen bir düzeltmeyle durumu kötüleştirmiş (AWS2, AWS3).
- Önlem (§6.5, s. 85-86): yeniden deneme üst sınırı, zaman aşımı ayarı, yük atma; otomatik ölçekleme tek başına yetmez.

**(3) Ne ölçmüş.** İstek/cevap servisleri; metastabil durum için kriter: tetikleyici kalktıktan sonra sistemin aşırı yükte kalması. Terim taraması: reconnect, jitter, backoff, MQTT, WebSocket, session geçmiyor (0).

**(4) Bizdeki artılar.**
- Bu çerçevede boşta kalan bir **mekanizmayı somut ölçüme bağlıyoruz:** kesintide biriken iş + eşzamanlı bağlanma + bağlantı sonrası boşaltma. Bu bir "yeniden deneme fırtınası" değil, **depola-ve-ilet birikimi**; çerçevenin "yükseltme" kategorisine uyar (iş yükünün kesinti süresiyle orantılı artması).
- **Tetikleyici süresinin etkisini** ölçtük (Adım 10): üstel + B1'de tepe, kesinti süresiyle neredeyse doğrusal (15, 60, 180 sn: 1,9; 9,1; 20,0 sn); Huang'ın "uzun tetikleyici daha kırılgan" bulgusuyla **uyumlu**.
- MQTT bağlam, açık ölçüm düzeneği.

**(5) Bizdeki eksiler.**
- **Metastabilite iddia edemeyiz.** Bizim aşırı yükümüz çoğunlukla **geçici**: birikim boşalınca canlı gecikme normale dönüyor. TLS'te "ikinci dalga" gördük ama o da toparlandı; "tetikleyici kalktıktan sonra süresiz kalıcı" kanıtımız yok.
- Onların biçimsel modeli (teoremler) ve üç yeniden üretilmiş sistemi var; bizde teori yok.
- Onların gerçek olay verisi var; bizde yok.

**(6) Farkımız.** Huang ve ark. yeniden denemenin sistemi çöküşte tuttuğunu gösterir; biz yeniden bağlanma ve birikmiş iş boşaltmasının **geçici ama büyük** canlı gecikme yarattığını ve bunun bekleme kuralı ile boşaltma politikasına nasıl bağlı olduğunu ölçeriz. Terimi kullanırsak "metastabil çöküşe benzer / uyumlu" deriz, "metastabil" demeyiz.

**(7) Atıf cümlesi taslakları.**
- *Giriş/İlgili Çalışmalar:* "Yeniden denemelerin aşırı yükü sürdürebildiği, kamuya açık 21 büyük olay raporunun yarısından fazlasında görülmüştür [Huang22]; kurtarma çoğunlukla yük atmayı gerektirmiştir."
- *Tartışma:* "Kesintinin süresi arttıkça aşırı yükün büyümesi, tetikleyici süresinin kırılganlığı artırdığı bulgusuyla uyumludur [Huang22]; ancak bizim ölçümlerimizde aşırı yük geçicidir ve sistemin tetikleyici kalktıktan sonra kalıcı olarak çöktüğüne dair bir kanıt yoktur."
- *Gelecek çalışma:* "TLS'teki ikinci dalganın bir sürdürücü etkiye dönüşüp dönüşmediği, cihaz sayısı ve kesinti süresiyle birlikte ayrıca taranmalıdır."

**(8) PDF işaretçileri ve uyarılar.**

| İddia | Yer | PDF'te ara |
|---|---|---|
| 21 olay, yeniden deneme >%50 | §2.2, s. 74-75 | "21 metastable failures", "retry policy" |
| Daha uzun tetikleyici → daha kırılgan | §3.5.2, s. 80 | "longer triggers also increase" |
| 10 sn vs 9 sn tetikleyici | §5.2, Şekil 5, s. 83 | "Decreased trigger duration" |
| Sınırsız yeniden deneme durumu kötüleştirir | §6.3, s. 85 | "Fix to Break" |

*Uyarılar.* (a) Giriş cümlesinde "22" (özet) ile "21" (Tablo 1) farkı: 21 halka açık rapor + 1 Twitter vakası. (b) "Metastabil" kelimesini kendi sonuçlarımız için kullanmayın. (c) "Yarıdan fazlası" derken kaynaklarına sadık kalın: metin "50%'den fazla" diyor, listelenen olay sayısı 11/21.

---

## 4. Isaacs ve ark., "Analyzing Metastable Failures" (HotOS 2025): `[Isaacs25]`

**(1) Künye.** R. Isaacs, P. Alvaro, R. Majumdar, K.-K. Muniswamy-Reddy, M. Salamati, S. Soudjani, *HotOS '25*, Banff, 14-16 Mayıs 2025, 7 sayfa, doi:10.1145/3713082.3730380. Yerel: `isaacs_2025_hotos_analyzing_metastable.pdf`. **Baştan sona okundu.**

**(2) Ne söylüyor.**
- Metastabil çöküşe karşı hassas bölgeyi **önceden** bulmak için bir araç topluluğu: kuyruk teorisinden **sürekli zamanlı Markov zinciri (CTMC)**, **ayrık olay simülasyonu (DES)**, **emülatör** ve gerçek servis üzerinde **stres testi**; araçlar sıkı bağlı: somut olanlardan gelen ölçüm soyut modeli kalibre eder, soyut modeller de deneyleri yönlendirir (§1-2).
- Ortam: tek ana makine, bir iş parçacığı havuzu, **istek/cevap** servisi; gelecek çalışma olarak **merkezi olmayan, dedikodu (gossip) tipi** sistemlere genişletme planlanıyor (§2).
- CTMC sınırı: üstel varsayım ve kaba soyutlama yüzünden modelin **nicel tahminleri benzetime ve emülasyona göre ciddi şekilde sapabilir**; yeniden deneme yükseldikçe daha kötü (§3).
- Emülasyonda bile, simülatörde görünmeyen sapmalar (yeniden deneme oranı %20 daha yüksek) çıkmış: emülasyon gerekli (§5).
- **Gerçek sistemde stres testi, en inandırıcı ve eyleme dönüştürülebilir çıktıdır;** modelleme, simülasyon ve emülasyonun gerçek sistemde doğrulanmadan pratik değeri yoktur (§6).
- Kalibrasyon: soyut bileşen somut komşusuna yakın davranmalı, uyuşmazlık modelde bir şeyin eksik olduğunu gösterir (§7).

**(3) Ne ölçmüş.** Tek servis örneği (5 RPS, kuyruk sınırı 150, tek iş parçacığı, 5 sn zaman aşımı, 5 yeniden deneme); yük 15 RPS ile 10 dakika aşırı yüklenip 5 RPS'e dönüş. Terim taraması: reconnect, jitter, backoff, MQTT geçmiyor (0).

**(4) Bizdeki artılar.**
- **Aynı ruhla basit model + ölçüm:** web gezginindeki model bir sunum aracı ve ölçümle sınanıyor.
- **Dürüst sınama:** modeli, kalibre edilmediği kesinti sürelerinde (Adım 10) önceden yazılmış tahminlerle sınadık ve bir hücrede **4,8 kat yanıldığını** raporladık. Isaacs ve ark.'nın uyarısıyla tutarlı.
- Gerçek servis (Mosquitto, EMQX) üzerinde ölçüm (onların "en inandırıcı" dediği adım).

**(5) Bizdeki eksiler.**
- Onların boru hattında **CTMC, DES ve emülatör** var; bizim modelimiz akışkan kuyruk ve elle kalibre. Bizde "somut → soyut kalibrasyon" döngüsü elle ve tek yönlü yapıldı.
- Metastabilite için biçimsel tanım ve analiz bizde yok.

**(6) Farkımız.** Onlar metastabil bölgeyi hesaplamaya çalışır; biz belirli bir MQTT düzeneğinde gecikmeyi ölçer ve basit bir modelin nerede tutup nerede tutmadığını gösteririz.

**(7) Atıf cümlesi taslakları.**
- *Tartışma (model sınırı):* "Basit bir modelin ölçümlerle kalibre edilip gerçek sistemde doğrulanması gerektiği, metastabilite analizi için de savunulmuştur [Isaacs25]; bizim modelimiz bu sınamada kalibre edilmediği kesinti sürelerinde güvenilir çıkmamıştır (Bölüm 5.12)."

**(8) PDF işaretçileri.** Tek istek/cevap odağı: §2 ("request-response"); model sapması: §3 ("deviate considerably"); gerçek sistemde doğrulama gerekliliği: §6 ("stress testing step has by far"); kalibrasyon: §7 ("Calibration"). *Uyarı:* Sayfa numaraları tablodaki iki sütunlu düzende karışık çıktı; atıfta **bölüm numarası** kullanın.

---

## 5. Floyd ve Jacobson (IEEE/ACM ToN 1994): `[Floyd94]`

**(1) Künye.** S. Floyd, V. Jacobson, "The Synchronization of Periodic Routing Messages," *IEEE/ACM Trans. Networking*, c. 2, s. 2, Nisan 1994, s. 122-136; SIGCOMM 1993 sürümünün genişletilmişi (SIGCOMM '93, s. 33-44). Yerel: LBL **ön baskısı** (sayfa numaraları ToN'dan farklı). **Giriş, model, rastgelelik rehberi ve sonuçlar okundu**; Markov zinciri ayrıntıları (§5.1-5.5) atlandı.

**(2) Ne söylüyor.**
- Bağımsız görünen çok sayıda periyodik işlem **istemeden eşzamanlı** olabilir; bu geçiş **ani bir faz geçişi**: eşik altında senkron değil, üstünde neredeyse kesin senkron (§1).
- Eşzamanlılığı önlemek için eklenmesi gereken rastgelelik **şaşırtıcı derecede büyük**: örnekte 300 ms işleme için en az yaklaşık 1 sn (§1).
- Rehber (§5.3, ön baskı s. 19; §7, s. 24): rastgele bileşen, işleme süresinin **en az on katı** olmalı (çıkarılmış metinde semboller kayıp, PDF'ten teyit edilecek); "her koşulda eşzamanlılığı ortadan kaldırır" diye önerilen basit kural: zamanlayıcıyı her turda `[0,5 Tp, 1,5 Tp]` aralığında düzgün rastgele seçmek.
- **İstemci-sunucu eşzamanlılığı** da tanımlanmış (§1, s. 2-3): bir sunucu arızadan ya da yoğunluktan toparlanırken istemciler **kurtarma işlemlerinde eşzamanlı hâle gelmiş** (Sprite dosya sistemi örneği) ve toparlanma ciddi şekilde gecikmiş. Bu, bizim problemin (sunucu kesintisi sonrası eşzamanlı yeniden bağlanma) **kavramsal atası**.
- Dış kuvvetle eşzamanlama (örn. elektrik arızasından sonra tüm yönlendiricilerin aynı anda başlatılması) açıkça anılıyor (§1, dipnot 2).
- Sonuç (§7, s. 24-25): küçük ölçekli davranış büyük ölçeğe aktarılamaz, eşzamanlılık belirli bir ölçekte ortaya çıkan bir özelliktir; **büyük ölçekli benzetimler gerekli**.

**(3) Ne ölçmüş.** Periyodik yönlendirme mesajları (RIP, IGRP, DECnet), simülasyon ve Markov zinciri; ağ ölçümleri (ping deneyi). Bizim konudan farklı: **sürekli periyodik** mesajlar, bizde **tek seferlik** eşzamanlama.

**(4) Bizdeki artılar.**
- **Gerçek sunucularda deneysel sınama**, rastgeleliğin **ne zaman işe yaradığı ve ne zaman yalnızca bedel getirdiği** (TLS'siz rahat ortamda rastgeleli kuralın bedeli, TLS'te ve çok cihazda faydası; Bölüm 5.5-5.8).
- **Kesinti süresiyle** güvenli bölgenin daraldığı (Adım 10: rastgeleli kural 180 sn'de 1,5 sn'ye çıkar); rastgelelik penceresinin yüke göre büyümesi gerektiğiyle **tutarlı olabilir**.

**(5) Bizdeki eksiler.**
- **Rastgelelik genişliğini taramadık.** Bizim rastgeleli kuralımız tek ayar (`U(0,5W, W)`, `W ≤ 30 sn`); Floyd-Jacobson'ın rehberiyle (geniş pencere, işleme süresinin katı) karşılaştırmadık.
- Teorik analiz bizde yok.

**(6) Farkımız / önerilen deney.** Floyd-Jacobson rastgelelik miktarını **kuramsal** olarak verir; biz belirli MQTT düzeneğinde ölçeriz. **Boşluğumuz:** rastgelelik penceresini (örn. W tavanı 10/30/60/120 sn ve pencere genişliği) tarayan bir deney yok; bu hem makalenin sınırı hem sıradaki iş.

**(7) Atıf cümlesi taslakları.**
- *İlgili Çalışmalar:* "Eşzamanlılığın kendiliğinden ortaya çıkabildiği, geçişin ani olduğu ve önlemek için gereken rastgeleliğin şaşırtıcı derecede büyük olduğu Floyd ve Jacobson tarafından gösterilmiştir [Floyd94]. Aynı çalışma, sunucu toparlanırken istemcilerin kurtarma işlemlerinde eşzamanlı hâle gelmesini bir örnek olarak anar."
- *Tartışma:* "Rastgeleli kuralın güvenli bölgesinin kesinti süresiyle daralması, rastgelelik penceresinin yükle birlikte büyütülmesi gerektiği görüşüyle tutarlıdır [Floyd94]; bunu doğrudan sınamadık (Bölüm 6.x, sınırlar)."

**(8) PDF işaretçileri ve uyarılar.**

| İddia | Yer | PDF'te ara |
|---|---|---|
| Ani faz geçişi | Özet, §1 | "phase transition" |
| Rastgelelik şaşırtıcı büyük, örnek 300 ms | §1 | "surprisingly large" |
| İstemci-sunucu eşzamanlılığı (Sprite) | §1, s. 2-3 | "Client-server models", "Sprite" |
| On kat rehberi ve `[0,5 Tp, 1,5 Tp]` | §5.3, s. 19; §7 | "ten times greater", "uniform distribution" |
| Küçük ölçek büyük ölçeğe aktarılamaz | §7 | "emergent property" |

*Uyarılar.* (a) Bu **ön baskı**; atıfta ToN künyesini kullanın (c. 2, s. 2, s. 122-136), sayfa göstermeyin. (b) Rehberdeki sembolleri (`Tr`, `Tc`) PDF'ten kontrol edin, metin çıkarımında kayboldu. (c) Konu **periyodik** mesajlar; bizimki tek seferlik. "Aynı problem" demeyin, "aynı mekanizma ailesi" deyin.

---

## 6. Bi, Zhang, Jiang, Guan (Applied Sciences 2026): `[Bi26]`

**(1) Künye.** H. Bi, Y. Zhang, J. Jiang, T. Guan, "Resilient End-Edge-Cloud Collaboration for Control Continuity and Closed-Loop Alarm Management in Solar Greenhouse IoT Systems Under Degraded Network Conditions," *Applied Sciences* 2026, 16(11), 5191, doi:10.3390/app16115191; yayın: 22 Mayıs 2026; CC BY. Heilongjiang Bayi Tarım Üniversitesi. Yerel: `bi_2026_applsci.pdf`. **Özet, yöntem, geri doldurma, sonuç, ablasyon ve sınırlılık bölümleri okundu.** (Önceki tarama notlarında "Bi ve ark." adıyla geçen çalışma budur; yazar ve künye bu okumayla doğrulandı. Eşleşmeyi kesinleştirmek için tarama dosyasında başlık yoktu: eşleşme "öncelikli, hız sınırlı geri doldurma" tanımından çıkarıldı.)

**(2) Ne söylüyor.**
- Sera IoT'sinde uç geçit ile bulut arasındaki kopmalar için bütünleşik bir mekanizma: ağ durumu, kontrol yetkisi (kira), uç geçitin kontrolü devralması, yerel tamponlama, **kurtarma aşamasında geri doldurma**, alarm yaşam döngüsü.
- **Geri doldurma (§3.3, s. 10-11):** bağlantı dönünce **her şeyi hemen yüklemek** patlamaya (burst), tıkanmaya ve yüksek öncelikli olayların gecikmesine yol açar; bu yüzden **olay-öncelikli, kademeli hız sınırlı, fark tabanlı geri doldurma** uygulanır: önce alarm ve kontrol özetleri, sonra kalan bant genişliğiyle toplu normal veri. Parti numarası, dizi numarası, toplu onay ve **olay zamanı ile varış zamanını ayırma** var.
- **Sonuç (§5.1, s. 18; §5.6 Tablo 7, s. 21):** Patlama tabanlı geri doldurma ile karşılaştırıldığında zirve verim 2,16'dan 0,69 MB/sn'ye, yüksek öncelikli olayların bitişi ≈ 17,3'ten 2,0 sn'ye, toplam eşitleme ≈ 19,9'dan 14,5 sn'ye iner. **Ablasyonda** (hız sınırlı ama önceliksiz) yüksek öncelikli olayların bitişi 13,9 sn'de kalır: hız sınırı patlamayı, **öncelik ise yüksek öncelikli olayların erken bitişini** getirir.
- Doğrulama: **simülasyon** (Python 3.11, 240 sn, 1 sn adım, 20 tekrar) ve halka açık bir 5G test yatağı ölçüm izi (§4.3, s. 14-15; §5.5). Gerçek sera, donanım döngüsü ve saha doğrulaması **yapılmamış** (§5.8, §6, s. 22-23).

**(3) Ne ölçmüş.** Zirve geri doldurma verimi, toplam eşitleme süresi, yüksek öncelikli olay tamamlanma süresi, ACK başarı oranı, uçtan uca gecikme dağılımı (olay düzeyinde), sera sıcaklığı sapması. **Canlı mesaj gecikmesi, MQTT sunucusu, çok sayıda cihaz ve bekleme kuralı yok.**

**(4) Bizdeki artılar.**
- **Çok cihaz** (500-10.000), **gerçek MQTT sunucuları** (Mosquitto, EMQX), **canlı alarm gecikmesi** (p99, saniyelik) ana ölçütümüz.
- **Olumsuz sonuçlar:** B3'ün (alarm önce) her ortamda B2'den iyi olmadığını gösterdik: üstel dönüş ve TLS'te canlı alarm B2'ye göre 2,3 kat kötü; N = 10.000'de varsayılan B2 ayarı yetersiz. Bi ve ark.'nın tek geçitli simülasyonu bu etkileşimleri göremez.
- **Birikmiş alarmın yaşı** (p50/p95/en büyük) ve **canlı alarmın gecikmesi** ayrı ölçülüyor.
- Bekleme kuralı ile politikanın **etkileşimi** (rastgeleli dönüşte B1 yeterken üstelde yetmez).

**(5) Bizdeki eksiler.**
- **B3 fikri yeni değil:** "önce yüksek öncelik, hız sınırlı geri doldurma" Bi ve ark.'da (2026) açıkça tarif ve ablasyonla değerlendirilmiş. Bizim B3'ümüz (alarm hemen, vital B2 gibi yayılır) ona yakın.
- Onlarda **tekrar-önleme** (dizi numarası, toplu onay), **olay zamanı / varış zamanı ayrımı** ve **sınırlı tampon alanı için öncelikli saklama** var; bizim cihaz kuyruğumuz sınırsız ve alıcı tarafında yalnızca mesaj numarasıyla kayıp/tekrar sayıyoruz.
- Bizim çalışma sanal cihazlı ve tek makineli; onların simülasyonu da öyle, ama onlar bunu açıkça "gerçek platformda doğrulanmadı" diye sınırlıyor; biz de benzer sınır yazıyoruz.

**(6) Farkımız.** Bi ve ark. öncelik-önce ve hız sınırlı geri doldurmayı tek bir uç geçit için simüle eder; biz aynı fikirleri **çok cihazlı, gerçek MQTT sunucusunda, canlı alarm gecikmesiyle ölçer ve hangi koşulda işe yaradığını, hangisinde yaramadığını gösteririz.** Bizim katkımız **fikir değil değerlendirme**.

**(7) Atıf cümlesi taslakları (Giriş madde (vi) için düzeltilmiş hâl):**
- "B3, kesintide biriken alarmların önce, diğer ölçümlerin ise yayılarak gönderildiği küçük bir politikadır. Öncelik-önce ve hız sınırlı geri doldurma fikri, sera IoT'si için tek bir uç geçitte simülasyonla değerlendirilmiştir [Bi26]; biz bu fikirleri çok cihazlı, gerçek MQTT sunucularında ve canlı alarm gecikmesi ölçütüyle değerlendirir, hangi koşullarda fayda ve bedel getirdiğini raporlarız."
- *Tartışma:* "Öncelikle gönderme yüksek öncelikli olayların bitişini belirgin biçimde öne çeker, hız sınırı ise patlamayı azaltır [Bi26]; bizim ölçümlerimiz bunun canlı alarm gecikmesi açısından ortama bağlı olduğunu gösterir (Bölüm 5.10)."

**(8) PDF işaretçileri ve uyarılar.**

| İddia | Yer | PDF'te ara |
|---|---|---|
| Hemen yükleme patlama yaratır; öncelikli ve hız sınırlı geri doldurma | §3.3, s. 10-11 | "event-prioritized, hierarchically rate-limited" |
| Önce yüksek öncelikli olaylara bant | §3.3, s. 11 | "first assigned to high-priority" |
| Olay zamanı / varış zamanı ayrımı | §3.3, s. 11 | "occurrence time and arrival time" |
| Sayılar: 2,16→0,69 MB/sn; 17,3→2,0 sn | §5.1, s. 18 | "2.16", "0.69" |
| Ablasyon: önceliksiz 13,90 sn, öncelikli 1,95 sn | §5.6, Tablo 7, s. 21 | "Ablation comparison" |
| Gerçek platformda doğrulanmadı | §5.8, §6 | "have not yet been validated" |

*Uyarılar.* (a) Bu çalışma **MQTT** bile kullanmıyor; "MQTT'de öncelikli boşaltma" yerine "uç geçitten buluta öncelikli geri doldurma" deyin. (b) B3'ü "yeni algoritma" diye sunmayın. (c) Sonuçlar simülasyon; mutlak sayılar alıntılanmamalı. (d) Çalışmanın bağlamı "sera"; tıbbi çerçevemizle doğrudan örtüşmez.

---

## 7. STGen (JSAN 2026): `[STGen26]`

**(1) Künye.** (Yazar listesi PDF'ten doğrulanmadı; makale başlığı ve künye MDPI'den) "Sensor Traffic Generator (STGen)", *J. Sens. Actuator Netw.* 2026, 15(4), 63, doi:10.3390/jsan15040063. CC BY. Yerel: `stgen_2026_jsan.pdf`. **Yalnızca özet, dağıtık deney (§6.7-6.8) ve geçerlilik sınırları (§8.2) okundu.**

**(2) Ne söylüyor.** Her sanal düğümün ayrı bir işletim sistemi süreci olduğu, hafif bir yazılım test düzeneği: 6.000 düğüm yaklaşık 1 sn'de başlatılmış, düğüm başına yaklaşık 99 KB. Canlı geniş alan ağında (üç iş istasyonu, Tailscale) MQTT QoS 0, 60 sn canlı tutma ve Mosquitto 2.0 ile **patlamalı paket kaybı canlı tutma zamanlayıcısını doldurup oturumu düşürür**; beş pencerede 34 bağlantı kaybı ve her biri 180-420 ms'lik yeniden bağlanma ve yeniden abone olma döngüsü gözlenmiş.

**(3) "Yeniden bağlanma fırtınası" ne anlamda.** Özette bu ifade geçiyor ama metinde bunlar **tek tek, bağımsız bağlantı kopmaları ve yeniden bağlanma döngüleri** (düğüm başına, patlamalı kayıp kaynaklı); **sunucu kesintisi sonrası eşzamanlı filo yeniden bağlanması değil.** Deneyde 20 düğüm.

**(4)-(6) Biz.** Artı: eşzamanlı filo yeniden bağlanması ve sonrası iş; bizde ayrı ölçüt. Eksi: bizim simülatörümüz tek .NET sürecinde (süreç başına bir cihaz yok); N = 10.000'de taban gecikmenin yükselmesi simülatör yükünü düşündürür (makalede "Bağlantı sonrası yük deneylerine özgü sınırlar", madde iii); STGen, tek süreçli üretecin darboğaz olabileceğini kendi sınırlarında yazıyor (§8.2). **Not:** onların "fırtına" kullanımı bizimkinden farklı; terim kirliliği olmaması için "fırtına" kelimesini kendi çalışmamız dışında bu kaynağa atıfla kullanmayın.

**(7) Atıf.** Yalnızca yük üreteci sınırı bağlamında: "Kendi simülatörümüzün tek süreçli olması, büyük N'de yük üretecinin kendisinin darboğaz olabileceği anlamına gelir; benzer bir uyarı [STGen26]'da da vardır." Özet dışında yeniden bağlanma fırtınası bulgusu olarak atıf **yapmayın**.

**(8) Uyarı.** Yazar listesini PDF'in ilk sayfasından teyit edin; bu dosya yalnızca kısmen okundu.

---

## 8. MQTT 5.0 standardı (OASIS, 7 Mart 2019): `[MQTT5]`

**(1) Künye.** *MQTT Version 5.0*, OASIS Standard, 7 Mart 2019. Yerel: `oasis_mqtt_v5.0.pdf`. İlgili bölümler okundu, terim taraması yapıldı.

**(2) Ne söylüyor (bizimle ilgili kısım).**
- **Neden kodları** (§3.2.2.2 Connect Reason Code, CONNACK): `0x89 Server busy` ("sunucu meşgul, sonra tekrar dene" anlamında), `0x9F Connection rate exceeded` (bağlantı hızı sınırı aşıldı), `0x9C Use another server`, `0x9D Server moved` (+ `Server Reference` ile yönlendirme: §3.2.2.3.16, §4.11 Server redirection). Bunlar DISCONNECT'te de geçerli (§3.14.2.1).
- **`Server Keep Alive`** (§3.2.2.3.14): sunucu, istemcinin canlı tutma süresini geçersiz kılar.
- **Oturum ve ileti ömrü:** `Session Expiry Interval` (§3.1.2.11.2), `Message Expiry Interval` (§3.3.2.3.3); akış denetimi için `Receive Maximum` (§3.1.2.11.3).
- **Olmayanlar:** özellik tablosunda **bekleme süresi bildiren bir özellik yok**; metinde "back-off" ve "jitter" **hiç geçmiyor** (sayım: 0); "retry" yalnızca ileti teslimi için (§4.4). Yani standart, sunucunun yeniden bağlanma anında istemciye "şu kadar sonra gel" demesini **sayısal olarak** taşımıyor.

**(4)-(6) Biz.** Artı: standardın sunduğu ipucu **zamansız**; yeniden bağlanma zamanlaması istemci politikasına kalıyor, biz bu politikaların sonuçlarını ölçüyoruz. Eksi: standardın sunabileceği araçlardan **kalıcı oturum** (`Session Expiry Interval`, sunucu tarafı kuyruk) ve **`Receive Maximum` / `Message Expiry Interval`** bizim çalışmada denenmedi (cihazlar `CleanStart = true`, QoS 1; Mosquitto'da `max_queued_messages 0`, `max_inflight_messages 100`).

**(7) Atıf cümlesi taslakları.**
- "MQTT 5.0, sunucunun yoğunluğunu `Server busy` ve `Connection rate exceeded` gibi neden kodlarıyla bildirmesine izin verir [MQTT5, §3.2.2.2]; ancak istemciye ne kadar beklemesi gerektiğini bildiren bir özellik tanımlamaz, geri çekilme ve rastgelelik yalnızca istemci politikasına bırakılmıştır."
- *Gelecek çalışma:* "Kalıcı oturumların sunucu tarafı kuyruğu ve `Receive Maximum` ile akış denetiminin, istemci tarafı boşaltma politikalarıyla karşılaştırılması."

**(8) İşaretçi ve uyarılar.** Neden kodları: §3.2.2.2 tablosu, "Server busy", "Connection rate exceeded"; yönlendirme: §3.2.2.3.16 ve §4.11. *Uyarı:* "Standartta yok" iddiası, **özellik tablosunun** ve metin taramasının sonucudur; standardın EK bölümlerini (örn. §4.13 hata işleme) satır satır okumadım. Cümleyi "tanımlanmamıştır" biçiminde, tarama kapsamı belirtilerek kurun.

---

## 9. RFC 6455 §7.2.3 ve RFC 5626 §4.5: `[RFC6455]`, `[RFC5626]`

- **RFC 6455** (Fette, Melnikov, Aralık 2011) §7.2.3 "Recovering from Abnormal Closure" (s. 44): kopma sonrası hemen ve ısrarla yeniden bağlanan çok sayıda istemci, sunucuya bir tür hizmet engelleme saldırısı yapmış olur; **ilk yeniden bağlanma rastgele geciktirilmeli (0-5 sn makul)**, sonrakiler kesilmiş ikili üstel geri çekilmeyle uzamalı. PDF'te ara: "Recovering from Abnormal Closure".
- **RFC 5626** (Jennings, Mahy, Audet; **editörler**; Ekim 2009) §4.5 "Flow Recovery" (s. 21-22): `W = min(max-time, base-time × 2^ardışık-hata)`; gerçek bekleme W'nin **%50-100'ü** arasında düzgün rastgele. **Varsayılanlar:** taban 30 sn (hepsi başarısızsa) ya da 90 sn, **tavan 1800 sn**.
- **Bizim kuralımız bu iki standardın şeklini alır; sabitler farklıdır:** ilk bekleme `U(0, 5 sn)`; sonrasında `U(0,5 W, W)`; **taban 1 sn, tavan 30 sn**. RFC varsayılanlarıyla ilk yeniden deneme, ilk başarısızlıktan 30-60 sn sonra olurdu ve tavana ulaşınca denemeler arası 15-30 dakika olurdu (RFC §4.5 son paragraf); kontrollü bir ölçüm için daha kısa sabitler seçildi. **Bu fark Bölüm 3.2'de açıkça yazılmalı.**
- **Sunucudan gelen bekleme ipucu SIP'te var:** RFC 5626 §4.5, 503 yanıtındaki `Retry-After` başlığının istemciyi daha uzun beklemeye yöneltebileceğini söyler. MQTT 5'te bunun sayısal karşılığı yok (bu dosyada Bölüm 8). Bu karşılaştırma İlgili Çalışmalar'da kullanılabilir: "SIP'te sunucu bekleme süresi bildirebilirken MQTT 5'te yalnızca neden kodu vardır."
- **Atıf cümlesi taslağı:** "Bekleme kuralları, [RFC6455, §7.2.3]'ün ilk denemeyi rastgele geciktirme ve [RFC5626, §4.5]'in `W` üst sınırı ve %50-100 rastgele seçim önerilerinden türetilmiştir; bu çalışmada taban 1 sn ve tavan 30 sn alınmıştır (RFC 5626'nın varsayılan taban 30/90 sn ve tavan 1800 sn değerlerinden farklı)."

---

## 10. Okunmayanlar ve yönlendirmeler (makaleye girmeden önce kontrol)

| Kaynak | Durum | Neden önemli |
|---|---|---|
| **PrioMQTT** (Patti, Leonardi, Testa, Lo Bello, *Computer Communications* 220:43-51, 2024) | **Yalnızca arama özeti** (iris.unict.it 403 verdi) | MQTT'de mesaj önceliği; UDP kullanıyor, yeniden bağlanma değil. B3 ile ilişkisi dolaylı |
| **Al Enany, Harb, Attiya**, "A New Back-off Algorithm with Priority Scheduling for MQTT Protocol and IoT Protocols," *IJACSA* 12(11), 2021 | **Yalnızca özet** (WebFetch) | **Sunucu tarafında**, "şüpheli" yayıncılar için üstel gecikme + öncelik; yeniden bağlanma değil, farklı problem. Adı benziyor, karıştırılmamalı |
| **Gelishvili ve ark.**, *Computational and Applied Science* 2026 | **Bulunamadı** (tarama notu: 20 istemci, sabit/jitterlı) | Oyuncak ölçek, ama düşmanca bir hakem anabilir |
| **Brooker**, "Exponential Backoff And Jitter" (AWS blog) ve Amazon Builders' Library | Okunmadı, gri literatür | Jitter'ın "tam / eşit / ilişkisiz" türleri; rastgeleli kuralımızı bunlarla adlandırmak için |
| **AWS IoT Core** MQTT belgesi | **Doğrulanmadı** (arama özeti) | Kalıcı oturuma dönen istemciye saklanan mesajların saniyede en çok 10 hızla iletildiği belirtiliyor; sunucu tarafı hız sınırı örneği. Sayfa okunmadan yazılmaz |
| **Timecho / Apache IoTDB** blog yazısı | **Gri literatür, doğrulanmadı** | Uç geçitin kesinti sonrası öncelik sırasıyla ve jeton kovasıyla geri doldurduğunu anlatıyor; B2/B3 fikirlerinin sahada kullanıldığına işaret. Atıf için uygun değil, fikir kaynağı |
| Tıbbi IoT çalışmaları (02 numaralı dosya) | Yalnızca arama özeti | Çerçevenin dayanağı |

---

## 11. Katkı iddialarımızın güncel hâli

| # | Eski iddia | Okumadan sonra |
|---|---|---|
| (i) | Tekrarlanabilir ölçüm ortamı ve kayıt | **Korunur.** |
| (ii) | Üç bekleme kuralının toplu yeniden bağlanmada karşılaştırması | **Korunur.** Standartlar [RFC6455, RFC5626] kuralları önerir, ölçümü yok; Floyd94 kuramsal; Spillway modeller. |
| (iii) | Ölçüm ortamının sonuçları çarpıtması (Bölüm 4) | **Korunur.** Hiçbir kaynakta karşılığı yok. |
| (iv) | Bağlantı sonrası yükün canlı alarm gecikmesine etkisi, ayrı ölçüm | **Korunur, "bildiğimiz kadarıyla" ifadesiyle.** [Domingues24] tek istemci ve canlıyı ölçüt almıyor; [Bi26] simülasyon. |
| (v) | Politika sıralamasının ortama bağlılığı, çekirdek eklemenin yetmemesi | **Korunur.** |
| (vi) | B3: alarm önce, "benzer değerlendirme bulunamadı" | **Düzeltilmeli.** Fikir [Bi26]'da var. İddia: çok cihazlı, gerçek sunucuda **değerlendirme** ve koşullu fayda. |
| (vii) | Sessiz kesinti | **Korunur.** Okunan kaynaklarda yok. |
| (viii) | Kesinti süresi | **Korunur,** [Huang22]'nin tetikleyici süresi bulgusuyla ilişkilendirilmeli. |

---

## 12. Makalede yapılacaklar (öncelik sırasıyla)

1. **Giriş (vi) cümlesi:** "benzer bir değerlendirmesi bulunamamıştır" kaldırılacak, §6'daki (7) taslağıyla değiştirilecek. *(Bu oturumda uygulandı.)*
2. **Bölüm 2 İlgili Çalışmalar:** altı yer tutucu (`[ ]`) bu dosyadaki (7) cümlelerinden doldurulur: Huang22+Isaacs25 (metastabilite), Xie26 (eşzamanlı yeniden bağlanma), Domingues24 (birikmiş boşaltma), Bi26 (öncelikli geri doldurma), Floyd94 (eşzamanlılık ve rastgelelik), MQTT5 (standartta boşluk). Tıbbi IoT: Domingues24'ün sağlık senaryosu bir dayanaktır; ek tıbbi çalışmalar okunmadan eklenmez.
3. **Bölüm 3.2 Yöntem:** RFC sabitleriyle fark (§9) ve eşitleme akışının [Domingues24] ile ilişkisi (§1 (7)).
4. **Giriş, Özet:** "ilk" ya da "hiç yapılmamış" ifadeleri taranacak; "bildiğimiz kadarıyla" ve arama kapsamı yazılacak (arama: Ekim 2026; kaynaklar: bu dosyadaki 10).
5. **Tartışma:** (a) Rastgelelik penceresi taranmadı [Floyd94]; (b) kalıcı oturum ve `Receive Maximum` denenmedi [MQTT5]; (c) sunucu tarafı hız sınırı denenmedi [Xie26, AWS belgesi doğrulanınca]; (d) tamponun sınırlı olduğu durum denenmedi [Bi26]; (e) model sınırı [Isaacs25, Xie26]; (f) metastabil iddiası yok [Huang22].
6. **Kaynakça:** `makale/kaynakca.md` güncellendi (Bi26, STGen26 eklendi).
7. **Sıradaki deney önerileri (bu okumadan çıkan):** (i) rastgelelik penceresi taraması (W tavanı ve genişlik); (ii) sunucu tarafı hız sınırı (EMQX'te var, Mosquitto'da yok) ile cihaz tarafı B2'nin karşılaştırılması; (iii) kalıcı oturum (`CleanStart = false`) ile sunucu kuyruğu; (iv) sınırlı cihaz tamponu ve öncelikli saklama.
