# Karar Defteri

Raporun "TasarÄ±m kararlarÄ± ve gerekÃ§eleri" bÃ¶lÃ¼mÃ¼ iÃ§in. Her karar: **ne, neden, ne zaman**. Karar deÄŸiÅŸirse eskisini silme, yeni satÄ±r ekle.

| # | Tarih | Karar | GerekÃ§e |
|---|---|---|---|
| 1 | 3 Ekim | Konu: toplu yeniden baÄŸlanma ve baÄŸlantÄ± sonrasÄ± yÃ¼k, MQTT 5. Ã–lÃ§Ã¼m Ã§alÄ±ÅŸmasÄ±, yeni algoritma deÄŸil | Bilinen stratejiler (rastgele bekleme) literatÃ¼rde var. Savunulabilir olan dar bir Ã¶lÃ§Ã¼m iddiasÄ± (`14`, `15c`) |
| 2 | 11 Ekim | **TÄ±bbi IoT Ã§erÃ§evesi** (hasta izleme): sanal cihazlar monitÃ¶r/sensÃ¶r, canlÄ± mesajlar vital bulgu ve alarm | DanÄ±ÅŸman hocanÄ±n yÃ¶nlendirmesi. Deney dÃ¼zeneÄŸi deÄŸiÅŸmiyor, anlatÄ± ve trafik profili deÄŸiÅŸiyor |
| 3 | 11 Ekim | Kapsam kÃ¼Ã§Ã¼ltÃ¼ldÃ¼: yalnÄ±zca **temiz kesinti**, N = 5.000 (gerekirse 2.000 ve 5.000), 5 tekrar, ~40 Ã§alÄ±ÅŸtÄ±rma | YayÄ±n hedefi yok, amaÃ§ saÄŸlam bir Ã§alÄ±ÅŸma Ã§Ä±karmak. Sunum KasÄ±m sonu / AralÄ±k baÅŸÄ±. Sessiz kesinti (Kademe 2) ve 15.000 cihaz zaman kalÄ±rsa eklenir |
| 4 | 11 Ekim | SimÃ¼latÃ¶r: **C# / .NET 8 + MQTTnet 5.2**, her cihaz bir `async` gÃ¶rev | KullanÄ±cÄ±nÄ±n yÄ±ÄŸÄ±nÄ±. Python/paho 500 istemcide GIL sorunu yaÅŸÄ±yor (`15b`, STGen). Async gÃ¶revler binlerce cihazÄ± hafif taÅŸÄ±r |
| 5 | 11 Ekim | Analiz iÃ§in Python (pandas, matplotlib) kullanÄ±labilir | SimÃ¼latÃ¶r Python olmayacak, yalnÄ±zca CSV analizi |
| 6 | 11 Ekim | Sunucu adayÄ±: **Mosquitto 2.1.2**. EMQX ile karÅŸÄ±laÅŸtÄ±rma Faz 1'de | Hafif, kurulumu kolay. Tek iÅŸ parÃ§acÄ±klÄ± olduÄŸu iÃ§in TLS el sÄ±kÄ±ÅŸmalarÄ± kuyruÄŸa girer, bu fÄ±rtÄ±na Ã§Ä±kmasÄ± iÃ§in avantaj |
| 7 | 11 Ekim | Bu adÄ±mlar iÃ§in **deney gÃ¼nlÃ¼ÄŸÃ¼** tutuluyor (`docs/deney-gunlugu/`) | Rapor ve sunum iÃ§in ham malzeme, tekrarlanabilirlik |
| 8 | 11 Ekim | Ä°lk adÄ±m TLS'siz ve kimlik doÄŸrulamasÄ±z kuruldu | Ã–nce temel Ã§alÄ±ÅŸsÄ±n. TLS'in fÄ±rtÄ±naya etkisi AdÄ±m 3'te Ã¶lÃ§Ã¼lecek |
| 9 | 13 Ekim | Bağlantı zaman aşımı simülatör kodunda `CancelAfter` ile zorlanır | MQTTnet `WithTimeout` yanıtsız SYN'de TCP bağlantısını sınırlamıyor; bekleme kuralı hiç çalışmıyordu (günlük 04, Bulgu 1) |
| 10 | 13 Ekim | Sunucu 2 CPU / 2 GB, simülatör 4 CPU / 3 GB, konteyner sınırı; `.wslconfig` değiştirilmedi | Makinenin genel ayarına dokunmadan sabit kaynak payı; Docker varsayılanı 12 CPU / ~8 GB olduğu gibi kayıtlı |
| 11 | 13 Ekim | Cihazlar sunucuya **IP ile** bağlanır (`-IpIleBaglan`) | Ad ile bağlanınca toparlanma 27-69 sn, IP ile 2,1 sn; fark Docker DNS artefaktı (günlük 04, Bulgu 5). Ad ile yapılan koşular (`v2`, `v2b`, `v3mac`) artefakt içeriyor |
| 12 | 13 Ekim | Sunucu konteynerine sabit MAC verildi | Gerçek sunucunun MAC adresi yeniden başlatmada değişmez. MAC'in gecikmeye etkisi sınandı: yok (Bulgu 5) |

## AÃ§Ä±k kararlar

- Sunucu seÃ§imi: Mosquitto mu EMQX mi (pilot sonrasÄ±).
- "CanlÄ± Ã¶nce, geÃ§miÅŸ sonra" stratejisi Ã§ekirdek B seÃ§eneklerine eklenecek mi.
- Alarm mesajlarÄ± ayrÄ± Ã¶lÃ§Ã¼t olacak mÄ± (tÄ±bbi Ã§erÃ§eve iÃ§in Ã¶neri, onay bekliyor).
- Docker'a verilecek CPU/bellek sÄ±nÄ±rÄ± (`.wslconfig`).


