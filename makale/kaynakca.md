# Kaynakça Durumu

Metne yalnızca **"doğrulandı"** olanlar girer. Durum değiştikçe tarihle güncellenir. Yerel kopyalar ve künye ayrıntıları: `kaynaklar/INDEX.md`; okuma notları: `kaynaklar/OKUMA_NOTLARI.md`.

## Doğrulandı (doğrudan kaynaktan, 11-18 Ekim 2026)

Okuma derinliği sütunu önemli: "tam" baştan sona, "kısmi" özet + ilgili bölümler + terim taraması demektir.

| # | Künye | Neyi destekliyor | Doğrulama ve okuma derinliği |
|---|---|---|---|
| [1] | I. Fette ve A. Melnikov, "The WebSocket Protocol," RFC 6455, IETF, Aralık 2011. | §7.2.3: ilk yeniden bağlanma rastgele geciktirilmeli (0-5 sn "makul"); sonraki denemeler kesilmiş ikili üstel geri çekilmeyle uzamalı | Bölüm metni okundu; **yazar adı ve tarih metin üstbilgisinden doğrulandı** (18 Ekim) |
| [2] | C. Jennings, Ed., R. Mahy, Ed. ve F. Audet, Ed., "Managing Client-Initiated Connections in the Session Initiation Protocol (SIP)," RFC 5626, IETF, Ekim 2009. | §4.5 "Flow Recovery": `W = min(max-time, base-time × 2^ardışık-hata)`; gerçek bekleme W'nin %50-%100'ü arasında düzgün rastgele. **Varsayılanlar taban 30/90 sn, tavan 1800 sn; bizim kuralda taban 1 sn, tavan 30 sn** | Bölüm metni okundu; üç ad RFC'de **editör** olarak geçiyor; tarih doğrulandı (18 Ekim) |
| [3] | M. Domingues, J. N. Faria ve D. Portugal, "Dimensioning payload size for fast retransmission of MQTT packets in the wake of network disconnections," EURASIP Journal on Wireless Communications and Networking, 2024:2, doi:10.1186/s13638-023-02327-3. | Tek istemcili biriken veri boşaltma (en iyi yük boyutu ≈ 13 MB); sunucu tetikli `SYNC_REQ/SYNC_REP`; canlı verinin yeniden iletimi etkilemediği; **çok istemcili eşzamanlı yeniden iletim gelecek çalışma olarak bırakılmış (Sonuç, s. 16-17)** | **Tam okundu** (18 Ekim). Gelecek çalışma cümlesi gözle görüldü |
| [4] | H. Xie ve ark., "Open the Floodgates in a Digital Twin: Experiences of Building Spillway for 100M+-User Signaling Storms in Cellular Core Network," Proc. ACM SIGCOMM 2026, s. 1779-1797, doi:10.1145/3789240.3829205. | Hücresel çekirdekte eşzamanlı yeniden bağlanma fırtınası; dijital ikiz + CN-DES + Bayesçi optimizasyon; optimize edilen kritik parametreler sunucu (ağ) tarafı hız sınırları (Tablo 1, 7); cihaz tarafı geri çekilme taban süresi, jitter aralığı ve deneme sayısı Tablo 7'de "kritik değil" (§3.2 metni ise geri çekilme aralıklarını da anıyor: tam tutarlı değil) | **Ana metin baştan sona okundu;** ekler ve tablolar taranmıştır. MQTT, IoT, backlog, alarm geçmiyor. Ekler hakemsiz. Künye ACM sayfasından doğrulandı |
| [5] | L. Huang ve ark., "Metastable Failures in the Wild," 16th USENIX OSDI, Temmuz 2022. | Özet: 22 metastabil çöküş, 11 kuruluş (Tablo 1'de 21 halka açık rapor); en yaygın sürdürücü etki yeniden deneme politikası (listelenen 11 olay); daha uzun tetikleyici daha kırılgan (s. 80); 10 sn tetikleyici çöküş, 9 sn kurtarma (s. 83) | **Baştan sona okundu** (ekteki ispatlar hariç). İstek/cevap servisleri; reconnect/jitter/backoff/MQTT geçmiyor. Sayfa aralığı 73-90 (Isaacs ve ark. kaynakçasından) |
| [6] | R. Isaacs ve ark., "Analyzing Metastable Failures," HotOS '25, Mayıs 2025, doi:10.1145/3713082.3730380. | Kuyruk modeli (CTMC) + ayrık olay simülasyonu + emülatör + stres testi ile metastabil bölgeyi önceden bulma; modelin nicel tahminlerinin sapabileceği, gerçek sistemde doğrulamanın en inandırıcı adım olduğu | **Baştan sona okundu.** İstek/cevap servisi; reconnect/jitter/backoff/MQTT geçmiyor |
| [7] | S. Floyd ve V. Jacobson, "The Synchronization of Periodic Routing Messages," IEEE/ACM Transactions on Networking, c. 2, s. 2, Nisan 1994. | Eşzamanlılığın ani faz geçişi olduğu; önlemek için gereken rastgeleliğin şaşırtıcı büyüklükte olduğu; sunucu toparlanırken istemcilerin kurtarma işlemlerinde eşzamanlı hâle gelmesi (§1, Sprite örneği); rehber: rastgele bileşen `[0,5 Tp, 1,5 Tp]` (§5.3, §7) | **Kısmi** (giriş, model, rehber, sonuç okundu; Markov zinciri §5.1-5.5 atlandı). Sayfa aralığı (122-136) arama sonucundan; LBL ön baskısında yok. Rehberdeki semboller PDF'ten teyit edilecek |
| [8] | *MQTT Version 5.0*, OASIS Standard, 7 Mart 2019. | Neden kodları (`0x89`, `0x9F`, `0x9C`, `0x9D`), `Server Reference`, `Server Keep Alive`, `Session/Message Expiry Interval`; **bekleme süresi veren özellik yok, metinde back-off ve jitter geçmiyor** | Neden kodları, özellik tablosu ve ilgili cümleler okundu (18 Ekim); standardın tamamı satır satır okunmadı |
| [9] | H. Bi, Y. Zhang, J. Jiang ve T. Guan, "Resilient End-Edge-Cloud Collaboration for Control Continuity and Closed-Loop Alarm Management in Solar Greenhouse IoT Systems Under Degraded Network Conditions," Applied Sciences, 16(11), 5191, doi:10.3390/app16115191, 22 Mayıs 2026. | Kopma sonrası olay-öncelikli, kademeli hız sınırlı, fark tabanlı geri doldurma; hız sınırının patlamayı, önceliğin yüksek öncelikli olayların erken bitişini sağladığı (ablasyon, Tablo 7). **B3 fikrine en yakın çalışma**; simülasyon, tek geçit, MQTT ve canlı gecikme yok | **Kısmen okundu** (özet, yöntem, geri doldurma, sonuç, ablasyon, sınırlılık). Önceki taramadaki "Bi ve ark. 2026" ile eşleşme tanımdan çıkarıldı |
| [10] | "Sensor Traffic Generator (STGen)," Journal of Sensor and Actuator Networks, 15(4), 63, doi:10.3390/jsan15040063, 2026. | Yük üreteci sınırları (tek süreçli üreteç, darboğaz uyarısı); "yeniden bağlanma fırtınası" ifadesi tek tek kopmalar için, filo yeniden bağlanması değil | **Yalnızca özet, dağıtık deney ve geçerlilik sınırları.** Yazar listesi PDF'ten doğrulanacak |

## Yazılım (sürüm kayıtları `00_ortam.md` içinde)

| Öğe | Sürüm | Not |
|---|---|---|
| Eclipse Mosquitto | 2.1.2 | Docker imajı `eclipse-mosquitto:2`, özet `00_ortam.md`'de |
| MQTTnet | 5.2.0.1603 | NuGet |
| .NET | 8.0 | Çalışma zamanı konteyner imajı |
| Docker Engine | 28.4.0 | Docker Desktop, WSL2, çekirdek 5.15.167.4 |

## Doğrulanacak (okunmadan metne girmez)

| Kaynak | Neden gerekli | Not |
|---|---|---|
| Patti ve ark. 2024 (PrioMQTT), Computer Communications 220:43-51 | MQTT'de mesaj önceliği (UDP) | Yalnızca arama özeti; yayıncı sitesi 403 verdi |
| Al Enany, Harb, Attiya 2021, IJACSA 12(11) | Sunucu tarafı geri çekilme + öncelik (şüpheli yayıncılar) | Yalnızca özet; yeniden bağlanma değil, adı benziyor |
| Gelishvili ve ark., Comput. Appl. Sci. 2026 | 20 istemci, sabit ve jitterlı yeniden deneme (tepe 3,1× ve 1,4×; tarama notu) | **Aramada bulunamadı**; oyuncak ölçek; hakem durumu belirsiz |
| Brooker (AWS blog), Amazon Builders' Library | Jitter türleri (tam, eşit, ilişkisiz) | Gri literatür; okunmadı |
| Tıbbi IoT çalışmaları (02, "Ek (11 Ekim)") | Çerçevenin dayanağı | **Yalnızca arama özeti**, makaleler okunmadı |

## Kullanılmayacak / dikkat

- `15b` ve `15c` dosyalarındaki künyeler bir yapay zekâ ajanının taramasından geliyor. **Tek tek doğrulanmadan kaynakçaya girmez.** 18 Ekim itibarıyla Spillway ve Domingues künyeleri doğrulandı (yukarıda); Domingues'in aktarılan içeriği (gelecek çalışma cümlesi, tek istemci, canlı verinin etkisi) bizim okumamızla örtüştü.
