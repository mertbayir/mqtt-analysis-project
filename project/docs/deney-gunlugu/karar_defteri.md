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
| 9 | 13 Ekim | Bağlantı zaman aşımı simülatör kodunda `CancelAfter` ile zorlanır | MQTTnet `WithTimeout` yanıtsız SYN'de TCP bağlantısını sınırlamıyor; bekleme kuralı hiç çalışmıyordu (günlük 04, Bulgu 1) |
| 10 | 13 Ekim | Sunucu 2 CPU / 2 GB, simülatör 4 CPU / 3 GB, konteyner sınırı; `.wslconfig` değiştirilmedi | Makinenin genel ayarına dokunmadan sabit kaynak payı; Docker varsayılanı 12 CPU / ~8 GB olduğu gibi kayıtlı |
| 11 | 13 Ekim | Cihazlar sunucuya **IP ile** bağlanır (`-IpIleBaglan`) | Ad ile bağlanınca toparlanma 27-69 sn, IP ile 2,1 sn; fark Docker DNS artefaktı (günlük 04, Bulgu 5). Ad ile yapılan koşular (`v2`, `v2b`, `v3mac`) artefakt içeriyor |
| 12 | 13 Ekim | Sunucu konteynerine sabit MAC verildi | Gerçek sunucunun MAC adresi yeniden başlatmada değişmez. MAC'in gecikmeye etkisi sınandı: yok (Bulgu 5) |
| 13 | 15 Ekim | Gecikme **tek yönlü saatle** (CLOCK_MONOTONIC) ölçülür; duvar saati kullanılmaz. Cihaz zamanlayıcısı erken uyanırsa mesaj planlanan andan önce gönderilmez | WSL2 duvar saati ~30 sn'de bir ±3 sn sıçrıyor, negatif gecikme üretti (günlük 06, Bulgu 1) |
| 14 | 15 Ekim | Ayrı **toplayıcı** konteyner (2 CPU / 1 GB) eklendi; sunucuda `max_queued_messages 0`, `max_inflight_messages 100` | Gecikmeyi uçtan uca ölçmek için; sunucunun toplayıcıya giden mesajı kuyruk sınırından atmaması için |
| 15 | 15 Ekim | Bağlantı sonrası politikalar **B0/B1/B2**; eşitleme ve boşaltma yalnızca **yeniden bağlanmada** | İlk açılış yeniden bağlanma fırtınası değildir; B0 yalnızca referans (veriyi atar) |
| 16 | 16 Ekim | Adım 5 matrisi **baştan** koşuldu; yarım kalan/silinen 4 koşu geçersiz sayıldı | Docker Desktop kapanması + devam betiğindeki hata (günlük 06, "Koşu akışı"). Veri silinen koşular yeniden üretildi, hiçbir koşu "tamir" edilmedi |
| 17 | 16 Ekim | `matris5.ps1`'e `-TamamlananiAtla` eklendi | Yarıda kesilen matrisi sürdürmek için; kullanılmadan önce kayıt defteri biçimine (adlar tırnaksız) göre düzeltildi |

## Açık kararlar

- Sunucu seçimi: Mosquitto mu EMQX mi (pilot sonrası).
- "Canlı önce, geçmiş sonra" stratejisi çekirdek B seçeneklerine eklenecek mi.
- Alarm mesajları ayrı ölçüt olacak mı (tıbbi çerçeve için öneri, onay bekliyor).
- Docker'a verilecek CPU/bellek sınırı (`.wslconfig`).


