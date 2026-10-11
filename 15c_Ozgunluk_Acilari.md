# 15c — Toplu Yeniden Bağlanma Projesi: Özgün Nokta Adayları

Tarih: 3 Ekim 2026
Kaynak: Bir ajan raporu. Standart metinleri ve birkaç makalenin tam metni ajan tarafından açıldı. Bir kısmı yalnızca özet veya alıntı düzeyinde. **Hepsini kendin kontrol et, özellikle sunumda kullanacağın cümleleri.**

---

## 0. Özet (önce bunu oku)

**Olgu eski ve standartlaşmış.** Bu yüzden "yeniden bağlanma fırtınasını keşfettik" diyemeyiz. Savunulabilir olan **dar bir ölçüm iddiası.**

| Kaynak | Ne diyor |
|---|---|
| **RFC 5390** (SIP, 2008) | Olguya "çığ yeniden başlatma" (avalanche restart) diyor. Şehir çapında elektrik geri gelince cihazların hepsinin birden bağlanması senaryosunu anlatıyor |
| **RFC 6455 §7.2.3** (WebSocket, 2011) | Yeniden bağlanmanın bir saldırı gibi sunucuyu boğabileceğini söylüyor. **Rastgele ilk gecikme (0-5 sn "makul") + kesilmiş ikili üstel geri çekilme** öneriyor |
| **Floyd ve Jacobson** (IEEE/ACM ToN, 1994) | Aynı anda başlayan zamanlayıcıların kendiliğinden senkronize kalması, rastgelelik gerekliliği |
| **Bronson ve ark.** (HotOS 2021) | Yeniden deneme fırtınası ve metastabil çöküş |

**Sonuç:** "Rastgele gecikme yardımcı olur" tek başına özgün **değil.** Ama aşağıdaki **dar ölçüm iddialarından** biri veya ikisi savunulabilir.

## 1. Standartlarda ne var? (ajan açıp okudu)

**MQTT 5.0**
- Sunucunun reddederken kullanabileceği **neden kodları:** 0x89 *Server busy*, 0x97 *Quota exceeded*, **0x9F *Connection rate exceeded***. 0x9C/0x9D + *Server Reference* ile başka sunucuya yönlendirme.
- **Server Keep Alive:** Sunucu, boşta kalan istemcileri daha erken atmak için kalp atışı süresini kendisi belirleyebilir.
- **Session Expiry, Will Delay,** yeniden bağlanınca yeniden gönderme.
- Tüm metinde **"backoff", "jitter", "thundering" geçmiyor** ve **CONNACK'te "şu kadar sonra tekrar dene" alanı yok.** Bu, kontrol edilebilir bir boşluk.

**HTTP ve CoAP:** RFC 9110 `Retry-After` (503, 3xx); RFC 6585 `429`; RFC 7252 (CoAP 5.03, Max-Age).

**WebSocket:** 1012 ve 1013 kapanış kodlarının IANA kaydı yalnızca "HYBI posta listesi arşivi"ni kaynak gösteriyor, bir RFC değil.

**3GPP:** TS 24.501 §5.3.9, T3346 değerlerinin cihazlar arasında **senkronize olmaması** gerektiğini söylüyor.

## 2. Adaylar tek tek

Aşağıdaki "karar", ajanın savunulabilirlik değerlendirmesi.

### (a) İki aşamalı toparlanma: bağlantı fırtınası ve veri eşitleme fırtınası
Cihazlar bağlanır, **sonra** kaçırdıklarını almak için durum eşitleme (anlık görüntü veya fark) ister. İkinci aşama genellikle birinciden ağırdır.
- **En yakın çalışmalar:** Adya ve ark., *Thialfi*, SOSP 2011 (hakemli, ajan okudu): geçiş maliyeti kalem kalem, "fırtına önleyici" teknik var, 380 bin istemcili yük devri deneyinde 1,6 kat fazla kapasite gerekmiş. Rotaru ve ark., *MigratoryData*, Middleware 2017 endüstri bildirisi (ajan okudu): geçmişi önbellekten tekrar oynatıyor, bir yük devri deneyinde "sürü etkisi" görmemiş (yeniden bağlanmalar "doğal olarak dağılmış").
- **Çakışma:** Kısmi. Kavram var, ama MQTT/WebSocket'te iki aşamayı ayırarak ölçen açık bir çalışma yok.
- **Karar:** Ölçüm olarak savunulabilir, kavram olarak değil.
- **Dürüst ifade:** *"Bildiğimiz kadarıyla MQTT/WebSocket sistemlerinde bağlantı aşamasını ve eşitleme aşamasını ayıran açık bir ölçüm yok; en yakınları Thialfi ve MigratoryData, bunlar tescilli sistemlerde bütün yük devrini raporluyor."*

### (b) Kalp atışı senkronizasyonu
Cihazlar kopmayı kalp atışı sınırlarında dalgalar hâlinde fark ediyor.
- **Yakın:** Floyd ve Jacobson, Thialfi (üç kaçırılan kalp atışı sonrası algılama), Chen-Toueg-Aguilera 2002.
- **Çakışma:** Mekanizmada yüksek, MQTT/WS'te yeniden bağlanma senkronluğunun ölçümünde yok.
- **Karar: Zayıf.** Sonuç öngörülebilir. **Başlık iddiası olmasın, açıklayıcı yan deney olsun:** Çökme/RST anında ve senkron algılama yaratır, sessiz ağ kopması ise dağıtır. Mekanizmayı iddia etme.

### (c) Sunucu yönlendirmeli yeniden bağlanma ile istemci tarafı rastgele gecikme
Sunucu "şu kadar sonra gel" der (`Retry-After`, MQTT 5 neden kodları ve Server Reference, WebSocket 1013) ve bunun etkisi ölçülür.
- **Yakın:** SIP (RFC 5390 REQ 14, RFC 6357, RFC 7339; arXiv 0807.1160 ve 1210.1505 ön baskıları): `Retry-After` "aç/kapa" gibi anlatılıyor, çöküşü önleyemiyor. Thialfi'nin küçülen oran stratejisi. IEEE TENCON 2023 (MQTT 5 Server Reference, yük dengeleme için, fırtına için değil). Nakao, *Come Back Later*, FINE 2026 (5G, yalnızca özeti görülmeye çalışıldı).
- **Çakışma:** Kavramda yüksek (SIP, 3GPP, HTTP), MQTT 5/WebSocket'te bulunmadı.
- **Karar:** Yalnızca MQTT/WS'e özgü olarak savunulabilir. **Dürüst alt bulgu:** MQTT 5'in standart ipuçları **zamanlama taşımıyor.**
- **Dürüst ifade:** *"Standartlaşmış ipuçlarının MQTT/WebSocket'te neyi yapıp neyi yapamadığını ölçüyoruz; kavram SIP ve 3GPP'de yerleşik."*

### (d) Biriken mesajları boşaltma fırtınası (depola-ve-ilet)
Cihazlar kesinti sırasında biriktirdikleri mesajları bağlanır bağlanmaz boşaltıyor.
- **En yakın:** Domingues, Faria, Portugal, EURASIP JWCN 2024 (ajan okudu): **tek istemcili** boşaltma çalışması. Canlı verinin boşaltmaya etkisi ihmal edilebilir çıkmış. **"Aynı anda yeniden iletim yapan istemci sayısı" gelecek çalışma olarak bırakılmış.** Bi ve ark., Applied Sciences (MDPI) 2026 (yalnızca özet): önce-öncelik, oran sınırlı doldurma simülasyonu; MDPI sayfası açılmadı.
- **Çakışma:** Kısmi. Mekanizma biliniyor, **çok istemcili MQTT ölçümü** boşluk.
- **Karar: En iyi desteklenen boşluk.**
- **Dürüst ifade:** *"Domingues ve ark. çok istemcili eşzamanlı boşaltmayı gelecek çalışma olarak bırakmış; biz bunu ölçüyor ve canlı-önce ile FIFO'yu karşılaştırıyoruz."*

### (e) Heterojen cihaz sınıfları ve öncelikli kabul
Pille çalışan ve şebekeden beslenen cihazlar; toparlanmada öncelik.
- **Yakın:** Sim ve Cho, IEEE Systems J. 2020 (öncelikli erişim sınıfı engelleme), Welsh ve Culler, USITS 2003, Patti ve ark. 2024 (PrioMQTT, mesaj önceliği, kabul değil).
- **Karar: Düşük.** Aşikâr bir ayar ve dizüstünde pil enerjisi ölçülemez.

### (f) Hiyerarşik topolojiler (kenar geçitleri ve aracılar)
- **Yakın:** Chien ve ark. 2019, Lima ve ark. ICSA 2025 (köprüler, arıza yok), arXiv 2603.21600 (ön baskı, toparlanma testi yok).
- **Karar:** Boş ama öngörülebilir (fırtına kenara taşınır) ve tek dizüstünde inandırıcı değil.

### (g) Uyarlamalı istemci stratejileri
- **Yakın:** Farkiani ve ark., arXiv 2510.04516 (ön baskı, 100 istemciye kadar, üstel geri çekilmeye göre %97,3 daha az 429), Tavori ve ark. arXiv 2511.23278, Mehan ve Saluja arXiv 2608.25403, Håstad-Leighton-Rogoff 1996 (geri çekilme kuramı).
- **Karar: İddia etme.** Çakışma yüksek ve 2025-2026'da arXiv hareketli, "ilk biz yaptık" riski.

### Ek açılar (ucuz, literatür bulunamadı)
- **(h) Vasiyet / varlık fırtınası:** Kesinti başında "son vasiyet" mesajları, toparlanmada "doğum" mesajları patlaması. İki aramada ölçüm bulunamadı.
- **(i) İstemci kütüphanelerinin uyumu:** Popüler MQTT/WS kütüphaneleri 0x89/0x9F/1013/`Retry-After`'a uyuyor mu? Yalnızca GitHub konuları bulundu. **Yalnızca bir arama yapıldı, daha çok doğrulanmalı.**

## 3. Savunulabilirlik sıralaması
**(d) > (a) > (c) > (b) > (e) > (f) > (g)**

## 4. Ajanın önerisi: tek tutarlı küçük katkı

> **Bağlantı sonrası yük ("post-reconnect load"):** (a) veri eşitleme + (d) biriken mesaj boşaltma, **bağlantı kabulünden ayrı olarak zamanlanır.**
> - **Karşılaştırma kolu (c):** jitter'sız / RFC 6455 tam jitter / sunucu hızlı (server-paced) yeniden bağlanma.
> - **Açıklayıcı şekil (b):** üç kesinti türü: sunucu öldür, sessiz düşme, planlı boşaltma.

**Ölçütler:** %99'un yeniden bağlanma süresi, canlı gecikmenin hedefin altına dönme süresi, tepe bağlantı hızı, başarısız bağlantılar, kayıp, **boşaltma sırasında canlı p99.**

**Dizüstü uyarısı:** Loopback ve yük üretici CPU çekişmesi yüzünden sonuçlar **göreceli**, mutlak değil. Bunu yazıda söyle.

## 5. Bu bize ne söylüyor? (benim yorumum)
- **Protokol seçimi:** Standart açıdan en zengin olan **MQTT 5** (neden kodları, Server Reference, Session Expiry, Will Delay). Cihaz filoları için doğal seçim. WebSocket'te standart neredeyse hiçbir şey tanımlamıyor.
- **Özgünlük küçük ama gerçek:** "Fırtına eski" ama **MQTT 5'te bağlantı aşaması ile bağlantı-sonrası yükü ayıran, canlı trafiği koruyan çok istemcili açık bir ölçüm** bulunamadı.
- **Kapsam riski:** (a)+(d)+(c)+(b) birlikte büyük. 15a'daki minimum deney (180 çalıştırma) bunun yalnızca bir parçası. Birleşik tasarım **parçalara bölünmeli** (önce çekirdek, sonra eklentiler).

## 6. Dürüst boşluklar (ajanın)
- ACM DL, IEEE Xplore, MDPI, ScienceDirect sayfaları açılamadı. Bi 2026, STGen (JSAN 2026, yeniden bağlanma fırtınalarından söz ediyor), TENCON makalesi, Sim ve Cho, PrioMQTT yalnızca özet veya alıntı düzeyinde doğrulandı.
- Semantic Scholar 429 verdi, **Google Scholar denenmedi.** arXiv sorguları yalnızca başlık ve özeti eşliyor, "bulunamadı" demek "yok" demek değil.
- Ücretli metinler okunmadı. Bir çalışma fırtınaları bir bölümün içinde ele almış olabilir.
- SIP "aç/kapa" alıntısı yalnızca alıntı düzeyinde.
- **Teslimden önce arXiv'i tekrar kontrol et.** İlgili ön baskıların birkaçı Ağustos 2026'dan, yeni biri çıkabilir.
- **Patentlerde rastgele gecikmeli yeniden bağlanma için kapsamlı önceki çalışma var.** Bu fikrin özgünlüğünü etkiler, ölçümün özgünlüğünü değil.
- Satıcı belgeleri (HiveMQ, EMQX, AWS) kanıt olarak kullanılmadı.
