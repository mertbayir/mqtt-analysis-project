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
| 18 | 16 Ekim | Adım 5b: N = 10.000, başlangıç yayılımı 20 sn **sabit** tutuldu, simülatör sınırı 4 CPU / 3 GB aynı | Yalnızca N değişsin; varış hızının iki katına çıkması ölçeğin parçası (günlük 07). B2 parametreleri (30 sn / 200 ms) N ile ölçeklenmedi, bu bir sınır olarak yazıldı |
| 19 | 16 Ekim | Adım 5c: B2 parametreleri (pencere/aralık) deney değişkeni oldu; hücre etiketi `b2-w<pencere>i<aralık>`; B0 N = 10.000'de kontrol olarak eklendi; izleme 180 sn | 07'de B2 ölçeklenmedi ve B0 bu ölçekte yoktu; sorunun kaynağını ayırmak için (günlük 08) |
| 20 | 16 Ekim | Kayıp araştırması için toplayıcı ham kaydı (`-HamKayit`, git dışı) ve bağlantı olaylarına mono zaman damgası eklendi; hücre: rastgeleli/B1/N = 5.000, 6 tekrar | 6/39 koşuda küçük kayıp var, nedeni bilinmiyor; ham kayıt olmadan açıklanamaz (günlük 09) |
| 21 | 16 Ekim | Adım 6: TLS (RSA-2048, test CA, **sertifika doğrulanmaz**), 18 koşu, izleme 150 sn | Sunucuyu bağlanma aşamasında gerçekten zorlamak (Adım 4'te zorlanmadı); doğrulama maliyeti ve TLS sürümü ölçüm dışı (günlük 10) |
| 22 | 16 Ekim | TLS matrisi tamamlandı (t1); sonuç: TLS ile sunucu zorlanıyor, rastgeleli kuralın yönü tersine döndü. Sonraki: bağlantı sonrası iş TLS'te (F1b) ve EMQX (F5) | Günlük 10 |
| 23 | 17 Ekim | Adım 6b: TLS + bağlantı sonrası iş; toplayıcı TLS'siz kalır; N = 5.000, 6 hücre, 18 koşu, izleme 180 sn | İki yükü birlikte ölçmek (günlük 11) |
| 24 | 17 Ekim | Adım 6b tamamlandı (tls6, 18 koşu; ilk deneme yarım kalıp devam ettirildi, yarım koşu iptal klasöründe). Sonuç: TLS'te en iyi politika B2, TLS'siz N = 5.000'de B1; en iyi politika ortama bağlı | Günlük 11 |
| 25 | 17 Ekim | Adım 7: EMQX 5.8.6 (imza sabit), 2 CPU / 2 GB (Mosquitto ile aynı pay) ve ayrı 4 CPU matrisi; `force_shutdown` kapalı; hata düzeltmeleri: istemci TLS'siz modda TLS seçeneğini açık bırakıyordu (düzeltildi, geçmiş koşular etkilenmedi), hazır olma probu 127.0.0.1'e bakıyor | Günlük 12 |
| 26 | 17 Ekim | Adım 7 tamamlandı (E1 18 + E2 12 koşu). Sonuç: ikinci izdiham EMQX'te de var, sıralama aynı, çekirdek eklemek yetmiyor; EMQX aynı CPU payında daha yavaş. Sonraki: alarm önceliği B3 (günlük 13), makale cilası, web arayüzü | Günlük 12 |
| 27 | 17 Ekim | Adım 8 tamamlandı (P1 9 + P2 12 koşu): B3 alarm önceliği birikmiş alarmın yaşını B1 düzeyine indiriyor, canlı alarmı B2 kadar koruyor; üstel + TLS'te canlı alarm bedeli var. Bloklar arası değişkenlik büyük: karşılaştırmalar aynı blokta. Sonraki: makale cilası son tur (ilgili çalışmalar sizde), web arayüzü, Şekil 1 | Günlük 13 |

## Açık kararlar

- Sunucu seçimi: Mosquitto mu EMQX mi (pilot sonrası).
- "Canlı önce, geçmiş sonra" stratejisi çekirdek B seçeneklerine eklenecek mi.
- Alarm mesajları ayrı ölçüt olacak mı (tıbbi çerçeve için öneri, onay bekliyor).
- Docker'a verilecek CPU/bellek sınırı (`.wslconfig`).


