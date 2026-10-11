# 15a — Toplu Yeniden Bağlanma Projesi: Yöntem ve Dizüstü Yapılabilirliği

Tarih: 3 Ekim 2026
Kaynak: Bir ajan raporu (kaynak sayfaları ajan tarafından açıldı; ben tek tek yeniden doğrulamadım). **Kendi gözünle kontrol etmeden sayıları yazıya geçirme.**
İşaretler: **TAHMİN** = ajanın kestirimi, kaynağı yok. **DOĞRULANMADI** = belgeden teyit edilemedi.

> Proje: *Çok cihazlı gerçek zamanlı sistemlerde bağlantı kesintisi sonrası toplu yeniden bağlanma: performans etkisi ve dayanıklı istemci/sunucu stratejileri.*
> Bu dosya yalnızca **nasıl ölçeceğimizi ve bilgisayarının neyi kaldıracağını** anlatır. "Özgün nokta" analizi ayrı dosyada (15b/15c) olacak.

---

## 1. Dizüstü kaç bağlantı tutar?

**Port sınırı (en önemli engel)**
- Bir bağlantı (istemci IP, sunucu IP, sunucu portu) üçlüsüyle ayırt edilir. Sınır bu üçlü için geçerli, global değil.
- **Linux** varsayılan geçici port aralığı 32768-60999: yaklaşık **28.232 bağlantı**.
- **Windows** varsayılan aralık 49152-65535: **16.384 bağlantı** (netsh ile ~64,5 bine kadar genişletilebilir).
- Bağlantı kapanınca TIME_WAIT: Windows 120 sn, Linux 60 sn.

**Sınırı aşma yolları**
- Birden fazla **istemci konteyneri**, her biri kendi IP'siyle (kullanıcı tanımlı Docker ağı + `--subnet`).
- Birden fazla **sunucu portu**.
- Konteyner başına `--sysctl net.ipv4.ip_local_port_range` ile aralığı genişletmek.
- emqtt-bench dokümanı: kaynak IP başına 64K'nın altında kal, birden fazla kaynak IP için `--ifaddr`.

**Bilinmeyenler**
- **Boştaki bağlantı başına bellek:** Resmî veya hakemli bir rakam bulunamadı (**DOĞRULANMADI**). Kendin ölç: bağlantı sayısı arttıkça bellek eğimine bak.
- Bir 2016 Mosquitto posta listesi anekdotu: 100 bin üstü bağlantı ama 50 bin civarında keepalive zaman aşımları ve tek çekirdek tavan. Anekdot, tek iş parçacıklı sunucu.
- 2026 tarihli bir arXiv ön baskısı (2603.21600) 10.000 istemci çiftine kadar 1-4 vCPU sanal makinede denemiş.

**WSL2 ve Docker Desktop**
- WSL2 sanal makinesi varsayılan olarak Windows RAM'inin %50'sini ve tüm mantıksal işlemcileri kullanır, NAT ağıyla.
- Yayımlanan portlar `com.docker.backend` üzerinden sanal makineye iletilir (ekstra yük). Konteyner köprü ağına ana makineden erişilemez.
- Ana makine ağı (host networking) Docker Desktop 4.34'ten itibaren isteğe bağlı.
- **Öneri:** Simülatör ve sunucu aynı Docker ağında kalsın, sıcak yolda yayımlanmış port kullanma.

**Tahmin (TAHMİN)**
| | Güven |
|---|---|
| WSL2 içinde ayarlı, hafif sunucu ve simülatörle **20-60 bin** boşta bağlantı rahat | Orta |
| ~100 bin zorlama | Düşük |
| 150 binden fazla olası değil | Düşük |

Fırtına sırasında **el sıkışma CPU'su** RAM'den önce darboğaz olur. Makine: 2 performans + 8 verimlilik + 2 düşük güç çekirdeği (14 iş parçacığı). **Bir günlük kalibrasyon** ile doğrula.

## 2. Cihaz simülasyon araçları

| Araç | Yeniden bağlanma modellemeye uygunluk |
|---|---|
| k6 | Yeniden bağlanma mantığı JS ile yazılabilir ama sanal kullanıcı başına 1-5 MB, ağır |
| Artillery | Yeniden bağlanma ve geri çekilme belgelenmemiş, özel fonksiyon gerekir |
| Locust | Yerleşik WebSocket yok, kütüphane sarmalanır |
| Gatling | `wsReconnect` var ama geri çekilme/jitter belgelenmemiş |
| Tsung | Ağır kurulum, WebSocket/MQTT eklentileri ikincil kaynağa göre deneysel |
| Thor | Yeniden bağlanma yok, 2020'den beri güncellenmemiş. **Kaçın** |
| emqtt-bench, MQTTX bench | Yalnızca sabit aralıklı yeniden bağlanma |

**En iyisi: özel bir simülatör** (Java 21 sanal iş parçacıkları, ya da async .NET veya Node). Nedenleri:
- **Geri çekilme ve jitter bağımsız değişken olur.**
- Cihaz başına durum ve sıra numarası tutarsın.
- "Planlanan" ve "gerçekleşen" deneme zamanını kaydedersin.
- Her başarısız denemeyi sınıflandırırsın.
- k6 veya emqtt-bench'i yalnızca çapraz kontrol için kullan.

> **Önemli bulgu (özgünlük için de):** RFC 6455 §7.2.3 zaten WebSocket istemcisi için **rastgele ilk gecikme (0-5 sn "makul") + kesilmiş ikili üstel geri çekilme** öneriyor. Yani "rastgele gecikme yardımcı olur" tek başına **özgün bir bulgu olmaz.** Özgünlük başka yerde aranmalı (15b/15c dosyaları).

## 3. Kesinti yaratma ve TCP'ye etkisi

| Yöntem | Ne olur (ajanın aktarımı) | Güven |
|---|---|---|
| `docker stop` / `kill`, sonra başlat | SIGTERM, 10 sn sonra SIGKILL. Süreç çıkınca soketler kapanır (FIN veya RST). WebSocket istemcisi Close çerçevesi gelmezse 1006 görür | Orta. tcpdump ile doğrula |
| `docker pause` | Süreç donar, çekirdek bağlantıları yine sürdürür. **Ağ kesintisi değil, uygulama durması** | Çıkarım (DOĞRULANMADI) |
| `docker network disconnect` / `connect` | Belge TCP etkisinden söz etmiyor. **Sessiz düşme** bekleniyor. Gönderen `tcp_retries2` (15) kadar dener, yaklaşık 924 sn. Keepalive varsayılanı 2 saat | Çıkarım. IP'yi `--ip` ile sabitle |
| Toxiproxy | TCP vekili. `timeout` 0: veri bırakılır; `reset_peer`: TCP RST benzetimi. Her iki bacakta TCP'yi sonlandırır, bir sıçrama ekler. Sunucu tüm istemcileri vekilin IP'si olarak görür, **port sınırı vekil-sunucu bacağına taşınır** | Belgeli. "enabled=false" mevcut bağlantılara etkisi DOĞRULANMADI |
| tc netem | Gecikme, kayıp, tekrar, bozulma, sıra karışıklığı. WSL2 çekirdeğinde `NETEM` modül olarak var, ama kullanıcılar "qdisc not found" bildiriyor | **DOĞRULANMADI.** `docker run --cap-add NET_ADMIN` ile sına |

**Sonuç:** Kopma algılama zamanlayıcılarından **kısa** bir kesinti fırtına yaratmaz, çünkü TCP hayatta kalır. Bu yüzden **arıza imzasını** (temiz kapanış mı, sessiz mi) ve **kalp atışı aralığını** kontrol etmek şart. MQTT sunucuları keepalive'ın 1,5 katı sonra bağlantıyı keser.

## 4. Ne ölçülür, nasıl?

Standart bir tanım bulunamadı. Aşağıdakiler ajanın tanımları (alıntı olmayanlar ona ait).

- **Hazır bağlantı:** Kurulmuş ve uygulama düzeyinde yankı testini geçen bağlantı (RFC 2647 benzeri).
- **Toparlanma süresi (TTR_p):** Servis geri geldiği andan, cihazların **%p'sinin** (50, 95, 99, 100) hazır olduğu ana kadar. Tavanda **sağdan sansürle** (örn. 180 sn'de dur).
- **Tepe bağlantı denemesi/sn:** Herhangi bir 1 sn ve 100 ms penceresindeki en yüksek sayı. İstemci tarafında (bağlantı başlatma) ve sunucu tarafında (kabul).
- **El sıkışma başarısızlık oranı:** Başarısız/toplam, sınıflı: zaman aşımı, reddedildi, sıfırlandı, 101 dışı, kimlik doğrulama.
- **Goodput:** Saniyede benzersiz mesaj.
- **Kayıp ve tekrar:** Cihaz başına sıra numarası + sunucu defteri.
- **Sunucu CPU ve bellek:** En az 1 Hz örnekleme, tepe ve %80 üstü süre.

**Açık ve kapalı döngü**
- Yeniden bağlanma fırtınasında deneme zamanları **ilkeden ve tohumdan** belirlenmeli, sunucu cevabından değil.
- **Koordine edilmiş atlama:** Yük üreticisi yavaş sunucuyu beklerse yavaş örnekleri eksik kaydeder. wrk2 planlanan zamandan ölçerek düzeltir. YCSB tarzı üreticilerde de aynı hata var (Friedrich ve ark.).
- **Pratikte:** Simülatörde bağlantı eşzamanlılığına tavan koyma, planlanan başlangıcı kaydet, üretici işlemci kullanımını %80'in altında tut.

**Isınma, tekrar, güven aralığı**
- Georges ve ark. (OOPSLA 2007): birden fazla sanal makine çalıştırması, n < 30 için Student-t aralığı.
- Barrett ve ark. (OOPSLA 2017): sanal makine/ölçüm çiftlerinin en çok %43,5'i kararlı duruma ulaşıyor. Isınmayı **varsayma, sına.**
- Metastabil çöküşler (Huang ve ark., OSDI 2022) ve Google SRE: rastgele üstel geri çekilme + yeniden deneme bütçesi.

**Dizüstü gürültüsü**
- Windows, kullanıcı hareketsizliğinden sonra ön plan uygulamasının QoS'unu düşürebilir. Microsoft ölçüm için `DisableUserPresenceQos` kayıt değerini öneriyor.
- `.wslconfig`'de açıkça `memory`, `processors`, `swap=0`, `autoMemoryReclaim=disabled` ayarla.
- Docker `--cpuset-cpus`.
- Şarja tak, saat frekansını ve sıcaklığı kaydet.
- Sunucu ve simülatör **aynı sanal makinede** olsun (tek saat).
- 2603.21600 ön baskısı toparlanma süresi ve güven aralığı **raporlamıyor.** Senin çalışman bu boşluğu doldurabilir.

## 5. Minimum deney tasarımı (tümü TAHMİN)

**Faktörler**
| Faktör | Düzeyler |
|---|---|
| Yeniden bağlanma politikası | (1) sabit 1 sn, (2) jitter'sız kesilmiş üstel geri çekilme (1-30 sn), (3) RFC 6455 tarzı: rastgele ilk gecikme U(0, 5 sn) + jitterlı üstel |
| Cihaz sayısı N | 5.000, 15.000, 30.000 (hep 4 istemci konteynerine bölünmüş) |
| Arıza imzası | Sunucu öldür-başlat (FIN/RST) ile ağ bağlantısını kes (sessiz) |

**Sabit:** Kesinti 30 sn, tek sunucu, TLS yok, kalp atışı 10 sn, toparlanma tavanı 180 sn.

**Çalıştırma:** 18 hücre × 10 tekrar = **180 çalıştırma**, 10 blokta, her blokta her hücre rastgele sırada bir kez. Her çalıştırmada yığını yeniden başlat, rastgele tohumu kaydet.

**Süre:** Çalıştırma başına en çok ~6,5 dk (60 sn yükseliş + 30 sn yerleşme + 30 sn taban + 30 sn kesinti + ≤180 sn toparlanma + 45 sn temizlik). Toplam ~19,5 saat + ~4 saat pilot ve kalibrasyon + ~%20 yeniden çalıştırma = **~27 saat.**

**Analiz:** Hücre başına medyan ve bootstrap %95 güven aralığı, oran etki büyüklüğü, **hiç toparlanmayan çalıştırmaların oranı.**

**En büyük 5 tehdit ve azaltma**
1. **Üretici veya Docker/sanal makine darboğaz olur** → basit yankı sunucusuyla kalibre et, CPU sabitle, planlanan/gerçekleşen zamanı kaydet, N'yi üretici tavanının altında tut.
2. **Hibrit çekirdek, ısı, güç, WSL2 gürültüsü** → şarj, açık `.wslconfig`, bloklu ve rastgele çalıştırma, frekans/sıcaklık günlüğü, önceden belirlenmiş ısıl daralma dışlama kuralı.
3. **Enjekte edilen kesinti etiketine uymaz** → her mekanizma için tcpdump ve `ss` ile FIN/RST/sessizlik doğrula, IP sabitle. Tek makine ve sıfır gecikmeli ağın genellemeyi sınırladığını yaz.
4. **İstatistiksel geçerlilik** (ağır kuyruklar, n = 10, bağımsız olmayan çalıştırmalar) → bloklama, parametrik olmayan güven aralığı, önceden kayıtlı hipotezler, sansür raporla.
5. **Dış geçerlilik** (tek sunucu, aynı sanal cihazlar, keyfî geri çekilme parametreleri) → zaman kalırsa tek aracıda (Mosquitto veya EMQX) tekrarla.

## 6. Dürüst boşluklar
- Boştaki bağlantı başına bellek ve bu işlemci için kapasite tavanı için **kaynaklı rakam yok.**
- `docker network disconnect` ve konteyner durdurmanın TCP etkisi belgelenmemiş, RFC'lerden **çıkarım.** tcpdump ile doğrula.
- netem'in Docker Desktop'ta çalışıp çalışmadığı ve Toxiproxy'nin devre dışı bırakıldığında mevcut bağlantılara etkisi doğrulanmadı.
- **Hakemli bir yeniden bağlanma fırtınası benchmark'ı ve toparlanma süresi için standart tanım bulunamadı.**
- Kalibera ve Jones (ISMM 2013) ve Hoefler-Belli makalesinin kendisi açılmadı. Bir MQTT sunucuları makalesi (Springer ECSA 2020) açılmadı.
- Bazı sayfalar (ACM DL, dblp, Semantic Scholar, ResearchGate, Intel PDF, Kent) açılamadı. Hiçbiri aşılmaya çalışılmadı.

## 7. Kaynaklar (ajan raporundan, kendin aç)

**Ağ ve çekirdek:** [Linux ip-sysctl](https://www.kernel.org/doc/html/latest/networking/ip-sysctl.html) · [RFC 9293 (TCP)](https://www.rfc-editor.org/rfc/rfc9293.html) · [Windows dinamik port aralığı](https://learn.microsoft.com/en-us/troubleshoot/windows-server/networking/default-dynamic-port-range-tcpip-chang) · [Windows ağ ayarları](https://learn.microsoft.com/en-us/biztalk/technical-guides/settings-that-can-be-modified-to-improve-network-performance) · [IP_BIND_ADDRESS_NO_PORT](https://man7.org/linux/man-pages/man2/IP_BIND_ADDRESS_NO_PORT.2const.html) · [fs sysctl](https://www.kernel.org/doc/html/latest/admin-guide/sysctl/fs.html) · [nf_conntrack](https://www.kernel.org/doc/html/latest/networking/nf_conntrack-sysctl.html)

**Docker, WSL, kapasite:** [docker run](https://docs.docker.com/reference/cli/docker/container/run/) · [bridge ağı](https://docs.docker.com/engine/network/drivers/bridge/) · [emqtt-bench](https://github.com/emqx/emqtt-bench) · [Mosquitto anekdotu](https://www.eclipse.org/lists/mosquitto-dev/msg01411.html) · [arXiv 2603.21600](https://arxiv.org/abs/2603.21600) · [WSL yapılandırma](https://learn.microsoft.com/en-us/windows/wsl/wsl-config) · [WSL ağ](https://learn.microsoft.com/en-us/windows/wsl/networking) · [Docker Desktop ağ](https://docs.docker.com/desktop/features/networking/) · [Docker Desktop ağ nasıl](https://docs.docker.com/desktop/features/networking/networking-how-tos/) · [host ağı](https://docs.docker.com/engine/network/drivers/host/) · [for-win #15032](https://github.com/docker/for-win/issues/15032) · [Docker Desktop ayarları](https://docs.docker.com/desktop/settings-and-maintenance/settings/)

**Araçlar:** [k6 büyük testler](https://grafana.com/docs/k6/latest/testing-guides/running-large-tests/) · [k6 websockets](https://grafana.com/docs/k6/latest/javascript-api/k6-experimental/websockets/) · [k6 timers](https://grafana.com/docs/k6/latest/javascript-api/k6-timers/) · [k6 açık/kapalı](https://grafana.com/docs/k6/latest/using-k6/scenarios/concepts/open-vs-closed/) · [Artillery WebSocket](https://www.artillery.io/docs/reference/engines/websocket) · [Artillery script](https://www.artillery.io/docs/reference/test-script) · [Locust](https://docs.locust.io/en/stable/testing-other-systems.html) · [Gatling injection](https://docs.gatling.io/concepts/injection/) · [Gatling WebSocket](https://docs.gatling.io/reference/script/websocket/) · [Tsung](https://github.com/processone/tsung) · [Thor](https://github.com/observing/thor) · [MQTTX](https://mqttx.app/docs/cli/get-started) · [JEP 444](https://openjdk.org/jeps/444) · [HdrHistogram](https://github.com/HdrHistogram/HdrHistogram)

**Protokoller ve kesinti:** [RFC 2647](https://www.rfc-editor.org/rfc/rfc2647.txt) · [RFC 6455](https://www.rfc-editor.org/rfc/rfc6455.html) · [MQTT 3.1.1](https://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html) · [docker stop](https://docs.docker.com/reference/cli/docker/container/stop/) · [RFC 1122](https://www.rfc-editor.org/rfc/rfc1122.html) · [docker pause](https://docs.docker.com/reference/cli/docker/container/pause/) · [cgroup v2](https://docs.kernel.org/admin-guide/cgroup-v2.html) · [network disconnect](https://docs.docker.com/reference/cli/docker/network/disconnect/) · [RFC 5461](https://www.rfc-editor.org/rfc/rfc5461.html) · [Toxiproxy](https://github.com/Shopify/toxiproxy) · [tc-netem](https://man7.org/linux/man-pages/man8/tc-netem.8.html) · [WSL2 çekirdek yapılandırması](https://github.com/microsoft/WSL2-Linux-Kernel/blob/linux-msft-wsl-6.6.y/arch/x86/configs/config-wsl) · [WSL issue 8584](https://github.com/microsoft/WSL/issues/8584)

**Yöntem:** [Schroeder ve ark., NSDI 2006](https://www.usenix.org/legacy/event/nsdi06/tech/full_papers/schroeder/schroeder.pdf) · [wrk2](https://github.com/giltene/wrk2) · [Friedrich ve ark.](https://dl.gi.de/handle/20.500.12116/918) · [Georges ve ark. (yazar kopyası)](https://dri.es/files/oopsla07-georges.pdf) · [Barrett ve ark., arXiv 1602.00602](https://arxiv.org/abs/1602.00602) · [Maricq ve ark., OSDI 2018](https://www.usenix.org/conference/osdi18/presentation/maricq) · [Hoefler-Belli sunumu](https://htor.inf.ethz.ch/publications/img/hoefler-scientific-benchmarking_aachen.pdf) · [Papadopoulos ve ark.](https://research.vu.nl/en/publications/methodological-principles-for-reproducible-performance-evaluation/) · [Huang ve ark., OSDI 2022](https://www.usenix.org/conference/osdi22/presentation/huang-lexiang) · [Metastabil çöküşler özeti](https://charap.co/metastable-failures-in-distributed-systems/) · [Google SRE: zincirleme çöküşler](https://sre.google/sre-book/addressing-cascading-failures/)

**Donanım ve gürültü:** [Intel Core Ultra 7 155U](https://www.intel.com/content/www/us/en/products/sku/237327/intel-core-ultra-7-processor-155u-12m-cache-up-to-4-80-ghz/specifications.html) · [Windows QoS](https://learn.microsoft.com/en-us/windows/win32/procthread/quality-of-service) · [Docker kaynak sınırları](https://docs.docker.com/engine/containers/resource_constraints/)


---

## Ek (3 Ekim): Gerçek test makinesi

Bu dosyadaki sayılar önce Claude'un kendi dizüstü bilgisayarına (HP ProBook) göre tahmin edilmişti. Deneyleri **senin dizüstünde** yapacağın için aşağıdaki değerlerle yeniden değerlendirildi. Bunlar da **tahmindir**; küçük bir deneme çalıştırmadan kesin sayı verilmez.

| Parça | Senin makinen | Ne anlama geliyor |
|---|---|---|
| İşlemci | Intel Core i7-11600H, 6 çekirdek / 12 iş parçacığı | Bu seri uzun süre yüksek güçte çalışabilir; sürekli yük altında ProBook'tan düşük kalması beklenmez |
| RAM | 15,7 GB | ProBook ile aynı. Asıl sınır Docker'ın alabileceği kısım (aşağıya bak) |
| Disk | 63 GB boş | Yeterli. Sunucu görüntüleri küçük (yüzlerce MB), sonuç dosyaları metin. Docker'ın sanal diski zamanla şişebilir, ara sıra temizlenir |
| İşletim sistemi | Windows 11 Home | Docker Desktop, WSL2 ile çalışır; Hyper-V gerekmez (doğrulanacak: `docker info`) |
| Kurulu araçlar | docker, dotnet, java, node | İstemci simülatörü için hepsi yeterli. Python durumu kontrol edilmedi, zaten Python önerilmiyor (15a, STGen bulgusu) |
| Ekran kartı (RTX 3060) | Var | Bu projede **kullanılmaz**, önemsizdir |

### Kontrol edilecekler (ilk küçük denemede)

1. **WSL2 bellek sınırı.** Docker Desktop, Windows'ta varsayılan olarak RAM'in yaklaşık yarısını (~7-8 GB) alır. Çok bağlantılı deneylerde yetmeyebilir. `.wslconfig` ile artırılabilir (örnek: 10 GB). Kaç bağlantıyı kaldırdığı küçük bir denemeyle ölçülür.
2. **Güç planı ve ısı.** Priz bağlı, "En iyi performans" modu, havalandırma açık. Dizüstü ısınınca işlemci yavaşlar ve **ölçümler tutarsızlaşır.** Her çalıştırmanın başında makinenin sıcaklığı benzer olmalı.
3. **Trafik yolu.** Yük üreten istemciler de **Docker ağının içinde** çalışmalı. Trafik Windows'un içinden geçerse Docker Desktop'ın ara katmanı yavaşlatabilir ve ölçüm sunucuyu değil o katmanı ölçer (tasarım önerisi, doğrulanmadı).
4. **Arka plan işleri.** Antivirüs taraması, tarayıcı, güncellemeler ölçüm sırasında kapatılır veya aynı koşulda bırakılır.
5. **Çekirdek paylaşımı.** 12 iş parçacığı sunucu ile istemciler arasında bölünür (örnek: sunucuya 4, istemcilere 6, kalanı sistem ve ölçüm kaydı). Bu bölüşüm tüm çalıştırmalarda **aynı** kalmalı.

### Sonuç (tahmin)

15a'daki en küçük deney planı (5 bin / 15 bin / 30 bin cihaz) **kâğıt üstünde bu makineye uyuyor.** 30 bin cihaz sınırda olabilir, WSL2 belleği ve çekirdek bölüşümüne bağlı. Önerim: önce 5 bin ile başla, her şey oturunca büyüt.
