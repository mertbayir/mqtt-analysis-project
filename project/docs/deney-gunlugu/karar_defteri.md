# Karar Defteri

Raporun "Tasarım kararları ve gerekçeleri" bölümü için. Her karar: **ne, neden, ne zaman**. Karar değişirse eskisini silme, yeni satır ekle.

| # | Tarih | Karar | Gerekçe |
|---|---|---|---|
| 1 | 3 Ekim | Konu: toplu yeniden bağlanma ve bağlantı sonrası yük, MQTT 5. Ölçüm çalışması, yeni algoritma değil | Bilinen stratejiler (rastgele bekleme) literatürde var. Savunulabilir olan dar bir ölçüm iddiası (`14`, `15c`) |
| 2 | 11 Ekim | **Tıbbi IoT çerçevesi** (hasta izleme): sanal cihazlar monitör/sensör, canlı mesajlar vital bulgu ve alarm | Danışman hocanın yönlendirmesi. Deney düzeneği değişmiyor, anlatı ve trafik profili değişiyor |
| 3 | 11 Ekim | Kapsam küçültüldü: yalnızca **temiz kesinti**, N = 5.000 (gerekirse 2.000 ve 5.000), 5 tekrar, ~40 çalıştırma | Yayın hedefi yok, amaç sağlam bir çalışma çıkarmak. Sunum Kasım sonu / Aralık başı. Sessiz kesinti (Kademe 2) ve 15.000 cihaz zaman kalırsa eklenir |
| 4 | 11 Ekim | Simülatör: **C# / .NET 8 + MQTTnet 5.2**, her cihaz bir `async` görev | Kullanıcının yığını. Python/paho 500 istemcide GIL sorunu yaşıyor (`15b`, STGen). Async görevler binlerce cihazı hafif taşır |
| 5 | 11 Ekim | Analiz için Python (pandas, matplotlib) kullanılabilir | Simülatör Python olmayacak, yalnızca CSV analizi |
| 6 | 11 Ekim | Sunucu adayı: **Mosquitto 2.1.2**. EMQX ile karşılaştırma Faz 1'de | Hafif, kurulumu kolay. Tek iş parçacıklı olduğu için TLS el sıkışmaları kuyruğa girer, bu fırtına çıkması için avantaj |
| 7 | 11 Ekim | Bu adımlar için **deney günlüğü** tutuluyor (`docs/deney-gunlugu/`) | Rapor ve sunum için ham malzeme, tekrarlanabilirlik |
| 8 | 11 Ekim | İlk adım TLS'siz ve kimlik doğrulamasız kuruldu | Önce temel çalışsın. TLS'in fırtınaya etkisi Adım 3'te ölçülecek |

## Açık kararlar

- Sunucu seçimi: Mosquitto mu EMQX mi (pilot sonrası).
- "Canlı önce, geçmiş sonra" stratejisi çekirdek B seçeneklerine eklenecek mi.
- Alarm mesajları ayrı ölçüt olacak mı (tıbbi çerçeve için öneri, onay bekliyor).
- Docker'a verilecek CPU/bellek sınırı (`.wslconfig`).
