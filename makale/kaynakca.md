# Kaynakça Durumu

Metne yalnızca **"doğrulandı"** olanlar girer. Durum değiştikçe tarihle güncellenir. Yerel kopyalar ve künye ayrıntıları: `kaynaklar/INDEX.md`; okuma notları: `kaynaklar/ANALIZ.md`.

## Doğrulandı (doğrudan kaynaktan, 11-18 Ekim 2026)

Okuma derinliği sütunu önemli: "tam" baştan sona, "kısmi" özet + ilgili bölümler + terim taraması demektir.

| # | Künye | Neyi destekliyor | Doğrulama ve okuma derinliği |
|---|---|---|---|
| [1] | I. Fette ve A. Melnikov, "The WebSocket Protocol," RFC 6455, IETF, Aralık 2011. | §7.2.3: ilk yeniden bağlanma rastgele geciktirilmeli (0-5 sn "makul"); sonraki denemeler kesilmiş ikili üstel geri çekilmeyle uzamalı | Bölüm metni okundu; **yazar adı ve tarih metin üstbilgisinden doğrulandı** (18 Ekim) |
| [2] | C. Jennings, Ed., R. Mahy, Ed. ve F. Audet, Ed., "Managing Client-Initiated Connections in the Session Initiation Protocol (SIP)," RFC 5626, IETF, Ekim 2009. | §4.5 "Flow Recovery": `W = min(max-time, base-time × 2^ardışık-hata)`; gerçek bekleme W'nin %50-%100'ü arasında düzgün rastgele. **Varsayılanlar taban 30/90 sn, tavan 1800 sn; bizim kuralda taban 1 sn, tavan 30 sn** | Bölüm metni okundu; üç ad RFC'de **editör** olarak geçiyor; tarih doğrulandı (18 Ekim) |
| [3] | M. Domingues, J. N. Faria ve D. Portugal, "Dimensioning payload size for fast retransmission of MQTT packets in the wake of network disconnections," EURASIP Journal on Wireless Communications and Networking, 2024:2, doi:10.1186/s13638-023-02327-3. | Tek istemcili biriken veri boşaltma (en iyi yük boyutu ≈ 13 MB); sunucu tetikli `SYNC_REQ/SYNC_REP`; canlı verinin yeniden iletimi etkilemediği; **çok istemcili eşzamanlı yeniden iletim gelecek çalışma olarak bırakılmış (Sonuç, s. 16-17)** | **Tam okundu** (18 Ekim). Gelecek çalışma cümlesi gözle görüldü |
| [4] | H. Xie ve ark., "Open the Floodgates in a Digital Twin: Experiences of Building Spillway for 100M+-User Signaling Storms in Cellular Core Network," Proc. ACM SIGCOMM 2026, s. 1779-1797, doi:10.1145/3789240.3829205. | Hücresel çekirdekte eşzamanlı yeniden bağlanma fırtınası; dijital ikiz + CN-DES + Bayesçi optimizasyon; sunucu tarafı hız sınırı (Tablo 7); cihaz tarafı geri çekilme/jitter parametreleri ayarlanmıyor | **Kısmi** (özet, Tablo 7, CN-DES/jitter bölümleri, terim taraması; MQTT, backlog, alarm geçmiyor). Künye ACM sayfasından doğrulandı |
| [5] | L. Huang ve ark., "Metastable Failures in the Wild," 16th USENIX OSDI, Temmuz 2022. | 22 metastabil çöküş, 11 kuruluş; en yaygın sürdürücü etki yeniden deneme politikası (olayların yarısından fazlası) | **Kısmi** (özet, giriş, terim taraması). İstek/cevap servisleri; reconnect/jitter/backoff/MQTT geçmiyor |
| [6] | R. Isaacs ve ark., "Analyzing Metastable Failures," HotOS '25, Mayıs 2025, doi:10.1145/3713082.3730380. | Kuyruk modeli + ayrık olay simülasyonu + emülatör ile metastabil bölgeyi önceden bulma | **Kısmi** (özet, giriş, terim taraması) |
| [7] | S. Floyd ve V. Jacobson, "The Synchronization of Periodic Routing Messages," IEEE/ACM Transactions on Networking, c. 2, s. 2, Nisan 1994. | Eşzamanlılığın ani faz geçişi olduğu; önlemek için gereken rastgeleliğin şaşırtıcı büyüklükte olduğu | **Kısmi** (özet, giriş, terim taraması). Sayfa aralığı (122-136) arama sonucundan; LBL ön baskısında yok |
| [8] | *MQTT Version 5.0*, OASIS Standard, 7 Mart 2019. | Neden kodları (`0x89`, `0x9F`, `0x9C`, `0x9D`), `Server Reference`, `Server Keep Alive`, `Session/Message Expiry Interval`; **bekleme süresi veren özellik yok, metinde back-off ve jitter geçmiyor** | Neden kodları, özellik tablosu ve ilgili cümleler okundu (18 Ekim) |

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
| **Bi ve ark., Applied Sciences (MDPI) 2026** | "Önce-öncelik, oran sınırlı doldurma"; **B3 (alarm önce) ile çakışabilir** | Yalnızca özet görüldü. B3 yenilik cümlesi bunu okumadan güçlendirilmemeli |
| Patti ve ark. 2024 (PrioMQTT) | MQTT'de mesaj önceliği | Okunmadı |
| STGen (JSAN 2026) | "MQTT yeniden bağlanma fırtınaları" adı geçiyor | Tarama notu: kanıtı zayıf; okunmadı |
| Gelishvili ve ark., Comput. Appl. Sci. 2026 | 20 istemci, sabit ve jitterlı yeniden deneme (tepe 3,1× ve 1,4×) | Oyuncak ölçek; hakem durumu belirsiz; okunmadı |
| Tıbbi IoT çalışmaları (02, "Ek (11 Ekim)") | Çerçevenin dayanağı | **Yalnızca arama özeti**, makaleler okunmadı |

## Kullanılmayacak / dikkat

- `15b` ve `15c` dosyalarındaki künyeler bir yapay zekâ ajanının taramasından geliyor. **Tek tek doğrulanmadan kaynakçaya girmez.** 18 Ekim itibarıyla Spillway ve Domingues künyeleri doğrulandı (yukarıda); Domingues'in aktarılan içeriği (gelecek çalışma cümlesi, tek istemci, canlı verinin etkisi) bizim okumamızla örtüştü.
