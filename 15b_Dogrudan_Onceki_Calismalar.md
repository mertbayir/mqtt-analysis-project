# 15b — Toplu Yeniden Bağlanma Projesi: Doğrudan Önceki Çalışmalar (okuma rehberi)

Tarih: 3 Ekim 2026
Kaynak: Bir ajan raporu (yaklaşık 85 sorgu: 41 web, 22 arXiv, 8 OpenAlex, 5 Crossref; Semantic Scholar 429 verdi, dblp bot kontrolü çıkardı). Ajan altı makalenin yayın yerini, yılını ve sayfa sayısını PDF kapaklarından ve Crossref'ten doğruladı, ilgili bölümleri okudu. **Sunumda kullanmadan önce her cümleyi kendin kontrol et.**

---

## 0. Hüküm (önce bunu oku)

**Konsept daha önce yapılmamış görünüyor, ama yapı taşları yapılmış ve 2026'dan çok yakın bir telekom makalesi var.** Ajan o makaleyi **okuyamadı.** Onu **ilk sen oku.**

| | Durum |
|---|---|
| Konsept (uygulama katmanında çok cihazın toplu yeniden bağlanması, MQTT/WebSocket) | Doğrudan ölçen hakemli çalışma **bulunamadı** |
| Yapı taşları (jitter, yeniden deneme bütçesi, yük atma, metastabil çöküş) | **Biliniyor ve yayımlanmış** |
| En yakın: telekom çekirdek ağında senkron cihaz yeniden bağlanması | **Spillway, SIGCOMM '26** (okunamadı) |

## 1. Okuma sırası (önerim)

| Sıra | Ne | Neden bu sırada | Süre |
|---|---|---|---|
| **1** | **Spillway** (SIGCOMM '26) | En yakın çalışma, ajan okuyamadı. **Senin üniversite ağınla açılabilir.** Konunu bununla ayırt etmen lazım | Özet ve giriş |
| 2 | **Isaacs ve ark.** (HotOS '25, 7 sayfa) | Kısa, yöntemi anlatıyor, kolay | Tamamı |
| 3 | **Huang ve ark.** (OSDI '22) | Klasik. Yeniden denemenin çöküşü nasıl sürdürdüğünü gösteriyor | §2, teoremler, §6.3 |
| 4 | **STGen** (JSAN 2026) | Adı "MQTT yeniden bağlanma fırtınaları" geçiyor, ama kanıtı zayıf. **Yük üreticinin tuzağını** gösteriyor | §6.2, 6.5, 6.8, 8.2 |
| 5 | **RFC 6455 §7.2.3** ve **RFC 5626 §4.5** | Standart kısımlar, birkaç sayfa. Rastgele gecikme zaten orada | Yalnızca o bölümler |
| 6 | Shen ve ark. SIP taslağı, Vilgelm ICC 2018 | İsteğe bağlı, farklı katman | İlgilenirsen |

## 2. Altı makale (ajanın aktarımı)

### 1) STGen (kısmi çakışma)
- **Künye:** Islam ve ark., *J. Sens. Actuator Netw.* 2026, 15(4), 63, 39 sayfa, doi:10.3390/jsan15040063. Hakemli (Ağustos 2026).
- **Ne yapıyor:** Düğüm başına süreç mantığıyla çalışan bir IoT emülatörü. **6.000 düğüm**, düğüm başına 99 KB. MQTT ile CoAP'ı NetEm altında ve 3 siteli Tailscale ağında karşılaştırıyor.
- **"MQTT yeniden bağlanma fırtınaları":** Özette bu ifade geçiyor ama kanıt, 20 düğüm ve QoS 0 ile beş 60 sn'lik pencerede 34 bağlantı kopma olayı. **Kesinti enjeksiyonu yok, geri çekilme yok, toparlanma süresi ölçütü yok.**
- **Yöntem uyarısı (bizim için önemli):** 500 paho istemcisinde Python'un GIL'i yüzünden teslimat çöktü (50.000 mesajdan 341'i). **Simülatörü Python'la yazma.**
- **Kendi sınırlılıkları:** §8.2(3) "yoğun dağıtımlar daha fazla inceleme gerektirir", §8.2(2) QoS 1/2 değerlendirilmedi.
- **Okurken bak:** "Fırtına" nasıl tanımlanmış? İstemci kütüphanesi doygunluğu, düğüm başına maliyet rakamları.

### 2) Huang ve ark., "Metastable Failures in the Wild" (kısmi)
- **Künye:** OSDI '22, s. 73-90, hakemli.
- **Ne yapıyor:** 21 kamuya açık olay raporu + bir Twitter vaka çalışması, üç yeniden üretim (çöp toplama, MongoDB 3 sn zaman aşımı + 4 yeniden deneme, ~6.200 RPS, memcached).
- **Bulgular:** Yeniden denemeler olayların **%50'sinden fazlasında** sürdüren etken. Yük atma azaltımların **%55'inden fazlasında.** %2'lik CPU değişimi veya 1 sn ek tetikleyici süre toparlanmayı tersine çevirebiliyor.
- **Kendi sınırlılıkları:** §2.2 "en iyi yargımızı kullandık", §4 raporlar "derinlik ve ayrıntıdan yoksun", §6.5 otomatik ölçekleme hızı "daha fazla araştırma gerektirir".
- **Boşluk:** Jitter veya geri çekilme varyantları denenmemiş, yalnızca istek/cevap sistemleri.
- **Okurken bak:** Teorem 1-4, AWS2 "sınırsız yeniden deneme" dersi, §6.3 "Fix to Break".

### 3) Isaacs ve ark., "Analyzing Metastable Failures" (kısmi, yöntem)
- **Künye:** HotOS '25, s. 172-178, hakemli atölye bildirisi.
- **Ne yapıyor:** CTMC (sürekli zamanlı Markov zinciri) + ayrık olay simülasyonu + emülasyon + stres testi araç zinciri. Örnek: 150'lik kuyruk, taban 5 RPS, 10 dakikalık 15 RPS dalgalanma.
- **Bulgular:** Hangi yapılandırmaların takıldığını ve beklenen toparlanma süresini tahmin ediyor. Emülatör, simülatörden %20 daha fazla yeniden deneme gösterdi.
- **Kendi sınırlılıkları:** §2 "gelecekte dedikodu tarzı sistemlere genişletmeyi planlıyoruz", §9 tetikleyici ve toparlanma süresi tahminlerinin doğruluğu.
- **Okurken bak:** Yörünge/kuyruk durum uzayı, araçlar arası kalibrasyon döngüsü, üstel dağılım varsayımları.

### 4) Mehan ve Saluja, "Retry Amplification…" (kısmi, **ön baskı**)
- **Künye:** arXiv 2608.25403v1, 26 Ağustos 2026, 6 sayfa. **Hakemli değil**, ikinci yazar bağımsız.
- **Ne yapıyor:** Yeniden deneme güçlenme faktörü (RAF) ölçütü + 200 Python deposu taraması (113 yapılandırmadan yalnızca 1'inde jitter doğrulandı) + 5 katmanlı zincir simülasyonu.
- **Bulgular:** İlişkili arızada standart yeniden deneme %41,5 başarı, hiç yeniden denememe %55,4. Uyarlamalı bütçelemeleri (ARB) yeniden denememe tabanıyla eşleşiyor.
- **Kendi sınırlılıkları:** §VIII-B "asenkron boru hatları ve toplu toparlanma ölçtüğümüzün dışında". Simülatör ağı anlık varsayıyor.
- **Dikkat:** "Standart" politika jitter'ı da içeriyor, yani jitter'ın tek başına etkisi **ölçülmemiş.**

### 5) Vilgelm ve ark., LTE RACH patlama çözümü (kısmi, farklı katman)
- **Künye:** IEEE ICC 2018, 7 sayfa, hakemli.
- **Ne yapıyor:** Elektrik kesintisinden sonra yeniden bağlanan sensörler için "patlama çözüm süresi". Stokastik ağ hesabı + OMNeT++, N = 100-1000 cihaz.
- **Kendi sınırlılıkları:** §III-B1 "yalnızca eşzamanlı etkinleşmenin en kötü durumunu ele aldık", §V diğer varış kalıpları "gerekli".
- **Okurken bak:** (birikim, süre, ihlal olasılığı) üçlüsü; sunucunun yayınladığı kabul ipucu olarak erişim sınıfı engelleme.

### 6) Shen, Schulzrinne, Koike, "SIP Avalanche Restart Overload Control" (kavramda kısmi-yüksek, ampirik olarak yok)
- **Durum:** draft-shen-soc-avalanche-restart-overload-07, Şubat 2014, 9 sayfa. **Süresi dolmuş bireysel taslak, "resmî statüsü yok" (gri literatür).**
- **Ne yapıyor:** Sunucu bir Restart-Timer ilan ediyor: (R/C)(1+k), k = 0,1. İstemciler ilk denemeyi [0, T] arası rastgele geciktiriyor. **Değerlendirme bölümü yok.**
- **RFC 5626 §4.5 (2009, Standards Track):** Yeniden deneme bekleme süresi W = min(1800, 30·2^başarısızlık), gerçek gecikme W'nin %50-100'ü arası rastgele.
- **Kendi sınırlılığı:** Etkinlik, "istemci sayısının öncesi ve sonrasında benzer kalması" varsayımına bağlı.
- **Okurken bak:** İlk deneme gecikmesi ile yeniden deneme gecikmesi farkı, kurallara uymayan istemcilere haksızlık (§5).

### **Geç bulundu, OKUNAMADI: Spillway**
- **Künye:** Xie ve ark., "Open the Floodgates in a Digital Twin: … Spillway … Signaling Storms in Cellular Core Network", **ACM SIGCOMM '26**, s. 1779-1797, doi:10.1145/3789240.3829205, **CC-BY (açık erişim).**
- **Özeti:** Senkron cihaz yeniden bağlanmasının "zincirleme, metastabil aşırı yüklenmeler" yarattığını söylüyor. Ayrık olay simülatörü, "fedakâr kısıtlama" (altruistic throttling) ve 5G Standalone ağında beş yıllık dağıtım anlatıyor.
- **Sorun:** ACM Cloudflare engeli verdi, ajan metne ulaşamadı. Çakışma değerlendirmesi **yalnızca özetten:** "kısmi-yüksek".
- **Bizim için fark (tahmin):** Bu **hücresel çekirdek ağ** sinyalleşmesi, bizimki **uygulama katmanı kalıcı bağlantı (MQTT/WebSocket)**. Ama bunu kendin okuyarak doğrulaman şart.

## 3. Sentez

### Zaten bilinenler
- **Kanonik çözüm:** Jitterlı geri çekilme, jittersizden iyi. Yeniden deneme bütçeleri ve yük atma yardımcı. Kaynaklar: Brooker'ın AWS yazısı (100 rakip istemci simülasyonu, çağrıları yarıdan fazla azalttı) ve AWS Builders' Library (**ikisi gri literatür**). RFC 5626 bunu 2009'da standartlaştırdı. Huang: yeniden denemeler olayların %50'sinden fazlasını sürdürüyor.
- **Geri çekilme yetmiyor:** Slack 2022 olay raporu (gri): istemciler geri çekilme + jitter kullandığı hâlde "otomatik yeniden denemeler yine yüke katkıda bulundu." Slack, istemci açılış isteklerini kısarak çözdü.
- **Kalıcı oturum pacing:** Maelstrom (OSDI '18) kalıcı mesajlaşma oturumlarının yeniden başlatmasını "sürü etkisinden kaçınmak için" hızlandırmış.
- **Kuram:** Geri çekilme kararlılık kuramı (Håstad ve ark. 1996; Bender ve ark. JACM 2018), yeniden deneme kuyruğu ve metastabilite modelleri.
- **Radyo katmanı:** Telekom katmanı kalabalık (Vilgelm, erişim sınıfı engelleme, Spillway).

### Açık kalanlar (her biri izlenebilir)
| | Boşluk | Dayanak | Güven |
|---|---|---|---|
| **O1** | Kalıcı bağlantılı cihaz filolarının (MQTT/WebSocket) senkron toplu yeniden bağlanmasını ölçen **hakemli çalışma bulunamadı** | STGen §8.2 sınırlılıkları; 2026 bir Valkey yazısı (gri) "benchmark'lar yeniden deneme davranışınızı modellemez"; arXiv'de "thundering herd", "connection storm", "reconnect storm" için sıfır isabet | **Orta** (ücretli metinleri göremiyorum) |
| **O2** | Yeniden deneme makaleleri bu kurulumu dışlıyor | Mehan ve Saluja §VIII-B; Huang ve Isaacs yalnızca istek/cevap | **Yüksek** (yazarlar kendileri söylüyor) |
| **O3** | Sunucunun ilan ettiği yeniden başlatma ipuçlarının **yayımlanmış ampirik değerlendirmesi yok** (2014 süresi dolmuş SIP taslağından sonra, o da değerlendirme bölümü içermiyor) | | **Orta** |
| **O4** | MQTT benchmark'ları toplu yeniden bağlanmayı kapsamıyor | mq-bench (arXiv 2603.21600, ön baskı): sabit 5 sn toparlanmayla 5-6 tekil kopma. Pham Le ve ark. (J. Inf. Security 2026): yayın seli sonrası toparlanma | **Orta-yüksek** |
| **O5** | Yeniden bağlanma varış zamanı dağılımları, eşzamanlı etkinleşmenin ötesinde araştırılmamış | Vilgelm, bunu gelecek çalışma olarak listeliyor | **Orta** |

### Özgünlük riski
- **Yüksek:** "Jitter anında yeniden denemeden iyidir" iddiası.
- **Orta:** "Bir fırtına simülatörü" iddiası.
- **Daha düşük:** Uygulama katmanında kalıcı bağlantılı sistemde **tekrarlanabilir, açık bir ölçüm çalışması.** **Yeni bir algoritma değil, ölçüm çalışması** olarak çerçevele ve **Spillway ile SIP taslağından açıkça ayrıştır.**

## 4. Dikkat edilecek yakın risk
**Gelishvili ve ark., *Computational and Applied Science* 2026**, 8 sayfa, hakem durumu belirtilmemiş: 20 istemci, 60 sn. Sabit 1 sn yeniden denemeyle tepe yük tabanın 3,1 katı, jitterla 1,4 katı bulunmuş. **Oyuncak ölçekli bir karşılaştırma**, ama düşmanca bir hakem, saf bir "sabit-jitter" iddiasına karşı bunu gösterebilir.

Diğer seçilmeyenler: bir Zenodo ön baskısı (Vasilev 2026, mobil istemciler için yeniden deneme sınırları) ve bir 2026 "cihaz sınıfına duyarlı jeton kovası" (5G-IoT) makalesi. İkisi okunmadı.

## 5. Dürüst boşluklar
- **Spillway okunmadı.** Grüner ve ark. (ICSA '21, MQTT broker dayanıklılık testleri, "MAYHEM") yalnızca özet düzeyinde görüldü.
- Netflix Zuul Push blogu 403 verdi, iddiaları doğrulanmadı.
- **Google Scholar yoktu.** WebSearch yalnızca ABD'den çalışan bir özetleyici, yayın yeri ve sayfa sayılarını PDF ve Crossref'ten doğruladım dedi.
- **Yokluğun kanıtı yok.** arXiv'de sıfır isabet, arXiv'in kök eşlemeli aramasından kaynaklanıyor olabilir. OpenAlex isabetleri yalnızca başlık ve özet.

İndirilen PDF'ler ve metinler ajanın oturum klasöründe: `C:\Users\LEDECH\AppData\Local\Temp\claude\C--Users-LEDECH-Desktop\c6ecebb4-ebcf-4dbf-bed7-a63149598413\scratchpad\pdf\`
