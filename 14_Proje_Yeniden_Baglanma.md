# 14 — Proje: Toplu Yeniden Bağlanma ve Bağlantı Sonrası Yük (KİLİTLENEN KONU)

Tarih: 3 Ekim 2026
Durum: **Konu kilitlendi** (3 Ekim, senin onayınla). Sunum türü: **Proje.**
Bu dosya yalnızca **form metni ve plandır.** Henüz hiçbir kod yazılmadı, hiçbir şey kurulmadı.
**TAHMİN** işareti: kaynağı olmayan, kestirimle yazılmış sayı. Pilot çalışma bunları doğrular veya değiştirir.

---

## 0. Özet (önce bunu oku)

- **Ne yapıyoruz?** Binlerce cihaz bir MQTT 5 sunucusuna bağlı. Sunucu ya da ağ kesilip geri gelince cihazların hepsi aynı anda dönmeye çalışır. Bunu yeniden üreten, ölçen ve karşılaştıran **tekrarlanabilir bir ölçüm ortamı** geliştiriyoruz.
- **Küçük ama gerçek katkı:** Yeniden bağlanma hakkında bilinen çok şey var (rastgele bekleme gibi). Bizim eklediğimiz: cihazlar **bağlandıktan sonraki** yük (kaçırdıklarını eşitleme + biriktirdiklerini boşaltma) **bağlanma aşamasından ayrı** ölçülür ve **ayrı zamanlanır.** MQTT 5 üzerinde, canlı trafik sürerken bunu yapan açık bir ölçüm **bulunamadı.** (Bulunamadı, "yok" demek değil.)
- **Ne değil?** Yeni bir algoritma önermiyoruz. Ölçüyoruz ve kime neyi borçlu olduğumuzu açıkça yazıyoruz.
- **İş miktarı:** 70-135 saat (TAHMİN). **Kademe 1** (temiz kesinti, çekirdek) bitince proje tamamdır. **Kademe 2** (sessiz kesinti) ve eklentiler üstüne gelir.
- **Sıradaki ilk adım:** Spillway'i oku (15b), sonra istersen küçük bir fizibilite pilotu (Bölüm 6, Faz 1). Formu 14-17 Ekim'de gönder.

---

## 1. Forma yazılacak metin

| Form alanı | Ne yazılacak |
|---|---|
| Öğrenci Adı Soyadı | Sen yaz |
| Araştırmak İstediğiniz Konu Başlığı | Aşağıdaki başlık |
| Konunun Kısa Özeti (zorunlu) | Aşağıdaki özet (uzunluk sınırı bilinmiyor, iki sürüm var) |
| Sunum Türü | **Proje** |

### Başlık (kilitlenen)

> **Çok Cihazlı MQTT 5 Sistemlerinde Toplu Yeniden Bağlanma ve Bağlantı Sonrası Yük: Bir Ölçüm Ortamı ve Strateji Karşılaştırması**

Başlık alanı kısaysa:

> Çok Cihazlı MQTT 5 Sistemlerinde Toplu Yeniden Bağlanma ve Bağlantı Sonrası Yük

### Kısa özet — standart sürüm (~120 kelime)

> Binlerce cihazın bir MQTT 5 sunucusuna bağlı olduğu sistemlerde sunucu ya da ağ kesilip geri geldiğinde cihazlar neredeyse aynı anda yeniden bağlanmaya çalışır ve bu, sunucunun toparlanmasını geciktirebilir. Bilinen önlem, cihazların bağlanmadan önce rastgele süre beklemesidir. Ancak bağlandıktan sonra da yük sürer: cihazlar kaçırdıkları durumu eşitler ve kesinti sırasında biriktirdikleri mesajları gönderir. Bu projede, açık kaynak bir MQTT 5 sunucusu ve kendi geliştireceğimiz bir cihaz simülatöründen oluşan, tekrarlanabilir bir ölçüm ortamı kurulacaktır. Farklı yeniden bağlanma stratejileri ile bağlantı sonrası yükün zamanlaması (hemen / ayrı zamanlı) karşılaştırılacak; toparlanma süresi, tepe bağlantı hızı, başarısız bağlantılar ve canlı mesaj gecikmesi ölçülecektir. Sonuçlar tek bilgisayarda alınacağı için göreceli olarak raporlanacaktır.

### Kısa özet — kısa sürüm (~55 kelime)

> Çok sayıda cihazın bağlı olduğu bir MQTT 5 sisteminde kesinti sonrası toplu yeniden bağlanmanın ve ardından gelen veri eşitleme ile biriken mesaj yükünün sunucuya etkisini ölçen, açık kaynak sunucu ve cihaz simülatöründen oluşan tekrarlanabilir bir ölçüm ortamı geliştirilecek; farklı yeniden bağlanma stratejileri toparlanma süresi ve canlı mesaj gecikmesi açısından karşılaştırılacaktır.

### Bu metni neden böyle yazdık?

| Seçim | Sebep |
|---|---|
| "İlk", "özgün", "yeni algoritma" gibi iddia yok | "Bulunamadı" ile "yok" aynı şey değil; teslimden sonra bir çalışma çıkarsa iddia yanlış kalmasın |
| Ürün **ve** ölçüm var ("ölçüm ortamı geliştirilecek") | Formda "Proje" neyi bekliyor bilmiyoruz. İkisini de içeren bir metin hangi okumada da geçerli |
| Formül, kısaltma yok | Form kısa serbest metin |
| "Tek bilgisayarda, göreceli" | Dizüstü sınırını baştan söylemek itirazı önler |

### Göndermeden önce (kontrol listesi)

- [ ] arXiv ve Google Scholar'da "reconnect storm", "thundering herd MQTT", "store-and-forward flush" ile son bir tarama (15c, 6. bölüm).
- [ ] Spillway'i okudum ve farkımızı tek cümleyle yazabiliyorum (Bölüm 3).
- [ ] Domingues ve ark. 2024'teki "gelecek çalışma" cümlesini **kendi gözümle** gördüm (en kritik tek cümle).
- [ ] Akıllı şehir 2209 projesinden kod, şablon ya da konu **taşımıyorum.**
- [ ] Formu 14-17 Ekim arası gönderdim. Son güne bırakma.

---

## 2. Proje yalın dille

Düşün: Bir sunucuya binlerce cihaz bağlı. Sunucu bir an kapanıp açılıyor. Cihazların hepsi aynı saniyede geri dönmeye çalışıyor, sunucu bu kalabalıkla boğuluyor, bazıları reddediliyor, onlar da tekrar deniyor ve sorun uzuyor.

Toparlanma **üç aşamada** olur:

```
Kesinti  ──►  1) BAĞLAN  ──►  2) EŞİTLE  ──►  3) BOŞALT  ──►  Normal
              cihaz geri       "ben yokken       "ben yokken
              gelir            ne değişti?"      biriktirdiğim
              (herkes          (durum bilgisi    mesajları
              aynı anda)       iste)             gönder"
```

- Çoğu çalışma **1. aşamayı** inceler: "cihazlar rastgele beklesin, kalabalık dağılsın."
- Biz **2. ve 3. aşamayı** ayrı inceleriz: Bağlanma dağılsa bile, bağlanan herkes hemen eşitlenip boşaltırsa **ikinci bir dalga** gelir mi? Bu dalga, **canlı akan mesajları** yavaşlatır mı?
- Sonra şunu dener: 2. ve 3. aşamayı bağlanmadan **ayrı zamanlarsak** (ek bekleme, saniyede kaç cihaz sınırı) ne kazanırız, neyi kaybederiz?

**Neden MQTT 5?** Standartta neden kodları ("sunucu meşgul", "bağlantı hızı aşıldı"), oturum ve biriken mesaj mekanizması hazır. Ayrıca MQTT 5 sunucunun cihaza "şu kadar sonra gel" diyebileceği bir alan taşımıyor; bunun pratikte ne anlama geldiği ölçülebilir.

---

## 3. Soru, hipotezler, katkı

### Araştırma soruları

| | Soru |
|---|---|
| **S1** | Toparlanma süresinin ne kadarı bağlanma aşamasından, ne kadarı bağlantı sonrası yükten (eşitle + boşalt) geliyor? |
| **S2** | Bağlanma aşaması rastgele beklemeyle yumuşatılınca bağlantı sonrası yük **ikinci bir dalga** yaratıyor mu? |
| **S3** | Bağlantı sonrası yükü bağlanmadan ayrı zamanlamak canlı mesaj gecikmesini ne kadar düzeltir, toparlanma süresine bedeli ne olur? |
| **S4** | Bulgular kesinti türüne (temiz / sessiz) göre değişiyor mu? (Kademe 2, yalnızca 5.000 cihazda. **Yön tahmin etmiyoruz, açık soru**) |

### Hipotezler (çürüyebilir, rapor çürüse de edilir)

| | Hipotez | Not |
|---|---|---|
| **H1** | Rastgele bekleme (RFC 6455 tarzı), bağlanma aşamasında tepe bağlantı hızını ve başarısız bağlantıyı düşürür | **Bilinen sonuç.** Ortamın doğru çalıştığının sağlaması. Çıkmazsa ortam hatalı |
| **H2** | Bağlanma dağılsa bile, bağlantı sonrası yük hemen başlarsa canlı mesaj gecikmesi (p99) bağlanma fırtınası geçtikten sonra da yüksek kalır | **Ana iddia.** Çürüyebilir |
| **H3** | Bağlantı sonrası yükü ayrı zamanlamak canlı p99'u düşürür; bedeli toplam toparlanma süresinin uzamasıdır | **Ana iddia.** Çürüyebilir |

Ek çıktı (hipotez değil): **devrilme noktası** N_kritik, yani toparlanmanın kademeli bozulmak yerine aniden çöktüğü en küçük cihaz sayısı.

**H2 çürürse ne olur?** "Bağlantı sonrası yük bu ölçekte ikinci dalga yaratmadı" da rapor edilebilir bir bulgudur. Ölçüm ortamı ürün olarak durur.

### Bilinen ve bizim katkımız

| Bilinen | Kaynak (ayrıntı 15b/15c) |
|---|---|
| Yeniden bağlanma fırtınası eski bir olgu | RFC 5390 (SIP, 2008, "avalanche restart"); Floyd ve Jacobson 1994 |
| Rastgele ilk bekleme + üstel geri çekilme standartlaşmış | RFC 6455 §7.2.3; RFC 5626 §4.5 |
| Yeniden deneme fırtınası çöküşe yol açabilir | Huang OSDI 2022; Isaacs HotOS 2025 |
| Telekom çekirdek ağında sinyalleşme fırtınaları | **Spillway**, SIGCOMM 2026 (**henüz okunmadı**) |
| Biriken mesaj boşaltma (tek istemci) | Domingues ve ark., EURASIP JWCN 2024. Çok istemcili eşzamanlı boşaltmayı gelecek çalışma bırakmış (**ajan okudu, sen doğrula**) |
| **Bizim eklediğimiz** | MQTT 5 üzerinde, canlı trafik sürerken, bağlantı sonrası yükün bağlanmadan ayrı ölçümü ve ayrı zamanlaması |

### Sunumda söylenecek dürüst cümle

> "Yeni bir algoritma sunmuyoruz. Bilinen stratejileri, bağlantı sonrası yükü ayrı hesaba katarak MQTT 5 üzerinde ölçüyoruz. Bildiğimiz kadarıyla bu ölçüm açıkça yayımlanmamış; en yakın çalışmalar şunlar ve farkımız şu."

### Söylenmeyecekler

- "İlk kez biz yaptık", "literatürde yok" (yalnızca: "bulamadık").
- "Rastgele bekleme işe yarar" bir **bulgu** olarak (bilinen şey; yalnızca sağlama).
- Tek dizüstünden çıkan mutlak sayılar ("sunucu 30 bin cihazı kaldırır").

### Kapsam dışı (bilerek)

Yeni algoritma, uyarlamalı (öğrenen) strateji, pil enerjisi ölçümü, hiyerarşik/çok sunuculu topoloji, gerçek donanım, akıllı şehir uygulaması.

---

## 4. Deney tasarımı (başlangıç değerleri TAHMİN, pilot ayarlar)

### Karşılaştırılacaklar

| Faktör | Düzeyler |
|---|---|
| **A. Bağlanma politikası** | **A1:** rastgelesiz üstel geri çekilme (1 → 30 sn). **A2:** RFC 6455 tarzı: ilk bekleme U(0, 5 sn) + rastgeleli üstel geri çekilme |
| **B. Bağlantı sonrası yük** | **B0:** yalnızca bağlan (referans). **B1:** hemen: bağlanır bağlanmaz eşitle + boşalt. **B2:** ayrı zamanlı: bağlandıktan sonra ek rastgele bekleme + saniyede en çok X cihaz |
| **N (cihaz sayısı)** | 5.000 ve 15.000 (30.000 eklenti, makine kaldırırsa) |
| **C. Kesinti türü** | **C1 temiz:** sunucu kabı durdurulup yeniden başlatılır (cihazlar kopmayı anında öğrenir). **C2 sessiz:** sunucu kabı ağdan çıkarılıp geri bağlanır (kimse haber almaz). **Bir çalıştırmada tek tür** olur, ikisi karıştırılmaz |

**Kesinti türleri iki kademede yapılır** (3 Ekim kararı):

| Kademe | Kesinti türü | Kapsam | Hücre × tekrar | Çalıştırma |
|---|---|---|---|---|
| **1 (çekirdek)** | C1 temiz | A × B × N (5.000 ve 15.000) | 12 × 10 | **120** |
| **2** | C2 sessiz | A × B, **yalnızca N = 5.000** | 6 × 10 | **60** |

- Kesinti türü karşılaştırması (C1 ve C2) **yalnızca 5.000 cihazda** yapılır. 15.000'de yalnızca temiz tür vardır.
- Kademe 2, çekirdek bitince başlar. Yalnızca Faz 1'de sessiz kesintinin **gerçekten sessiz olduğu doğrulanırsa** yapılır (ağ trafiği kaydıyla).
- Kademe 1 tek başına tam bir projedir.

### Kontrol çalıştırmaları (hafızadaki RRS-Bench değerlendirmesinden alınan ders)

| | Ne | Ne kanıtlar |
|---|---|---|
| **K1** | Kesinti yok. Aynı N cihaz kademeli, yavaşça bağlanıyor (örn. 200/sn) | Sorunun **sayıdan değil, aynı andalıktan** geldiğini. Aynı N yavaş bağlanınca sorun yoksa fırtına gerçektir |
| **K2** | Kesinti yok, yalnızca canlı trafik | Taban canlı gecikme. Karşılaştırmanın sıfır noktası |

K1 ve K2: 2 N × 2 kontrol × 5 tekrar = **20 çalıştırma.** Kontroller kesintisiz olduğu için kesinti türünden bağımsızdır.

**Toplam:** Kademe 1 = 120 + 20 = **140**; Kademe 2 = **60**; hepsi **~200 çalıştırma** (~23 saat makine zamanı, TAHMİN).

### Sabit tutulanlar (başlangıç değerleri, pilot ayarlar)

| Parametre | Başlangıç | Not |
|---|---|---|
| Kesinti süresi | 60 sn | Biriken mesaj sayısını belirler (~12/cihaz, TAHMİN). Sessiz kesintide cihazların kopmayı **fark etmesi** için kalp atışı aralığının çok üstünde olmalı (çok kısa kesinti fark edilmez, izdiham olmaz) |
| Cihaz okuma aralığı | 5 sn | Canlı trafik. Kesintide bellekte biriken mesajlar bundan |
| Bağlantı maliyeti | **TLS açık** | **Önemli:** TLS ve kimlik doğrulama yoksa bağlanma çok ucuz kalır ve fırtına **hiç çıkmayabilir.** Pilotta TLS'li ve TLS'siz karşılaştırılır |
| Kalp atışı (keepalive) | 10 sn | Sunucu kopmayı bunun 1,5 katı sonra anlar (15a) |
| İstemci bağlantı zaman aşımı | 5 sn | Zaman aşımı yeniden deneyi tetikler ve yükü artırır. **Mutlaka sabit ve raporlanır** |
| Toparlanma tavanı | 180 sn | Bu sürede toparlanamayan çalıştırma "hiç toparlanmadı" sayılır (sansür) |
| Sunucu kaynağı | Sabit sınır (örn. 2 çekirdek, 2 GB) | Tüm çalıştırmalarda aynı. Pilot ayarlar |
| İstemci | Kütüphanenin otomatik yeniden bağlanması **kapalı**, kendi mantığımız | Kütüphane kendi gecikmesini ekler ve deneyi bozar |

### Ölçülenler

| Ölçüt | Anlamı |
|---|---|
| **Toparlanma süresi (TTR)** | Servis geri geldiğinden cihazların %50 / %95 / %99'u hazır olana kadar. Tavanda sansürlü |
| **Tepe bağlantı denemesi/sn** | En yüksek 1 sn ve 100 ms penceresi; istemci ve sunucu tarafı |
| **Başarısız bağlantı oranı** | Sınıflı: zaman aşımı, reddedildi, sıfırlandı |
| **Canlı mesaj gecikmesi p99** | Boşaltma sürerken canlı mesajlar ne kadar gecikiyor. **Planlanan gönderim zamanından** ölçülür (yavaş sunucuda yavaş örnekler kaybolmasın diye, bkz. 15a) |
| **Boşaltmanın bitiş süresi** | Biriken mesajların hepsi ne zaman ulaştı |
| **Kayıp ve tekrar** | Cihaz başına sıra numarası + sunucu defteri |
| **Sunucu CPU / bellek** | 1 sn'de bir |
| **Hiç toparlanmayan oranı** | Tavanı aşan çalıştırmaların yüzdesi |
| **N_kritik** | Devrilme noktası |

**Yöntem kuralları (15a'dan):** Deneme zamanları sunucu cevabından değil **ilke ve tohumdan** belirlenir (açık döngü). Çalıştırmalar bloklu ve rastgele sırayla. Her çalıştırmada yığın yeniden kurulur, tohum kaydedilir. Medyan + bootstrap %95 güven aralığı. **Hipotezler deneyden önce dosyaya yazılıp saklanır.**

---

## 5. Ne geliştirilecek? (ürün kısmı)

| # | Bileşen | Not |
|---|---|---|
| 1 | **Cihaz simülatörü** | Öneri: **.NET** (senin yığının; MQTTnet MQTT 5 destekler, Faz 1'de doğrulanacak). Politikalar (A) ve zamanlama (B) takılabilir. Tohumlu rastgelelik. **Python/paho kullanma** (15a: 500 istemcide GIL çöküşü) |
| 2 | **Durum servisi** | "Eşitle" isteğine cevap veren küçük servis. Sunucudan **bağımsız** (sunucu öldürülünce o da gitmesin). Cevap boyutu ayarlanabilir |
| 3 | **MQTT 5 sunucusu** | Docker'da. **Mosquitto** (tek iş parçacıklı, hafif) veya **EMQX** (çok çekirdekli, ağır). Faz 1'de karşılaştırılıp seçilir |
| 4 | **Kesinti enjektörü** | Betik: sunucuyu öldür/başlat (temiz kesinti) ve ağ bağlantısını kes (sessiz kesinti). Her ikisi tcpdump/ss ile doğrulanır (15a) |
| 5 | **Toplayıcı** | CSV: planlanan/gerçekleşen zaman, sınıflı hata; sunucu metrikleri 1 Hz |
| 6 | **Çalıştırıcı ve analiz** | Blok-rastgele sıra, tohum kaydı, temizlik; analiz betiği (araç: sonra karar) |
| 7 | **Depo** | docker-compose + README. Tekrarlanabilirlik, sunumda ve tez için değerli |

**Tez bağlantısı:** Bu ortam, seneye tez için temel olabilir (örn. sunucu-hızlı yeniden bağlanma, kümelenmiş sunucu, gerçek cihaz).

---

## 6. Aşamalı plan (tarihsiz; takvim yalnızca form için sabit)

Sunum tarihi bilinmiyor (8. haftadan itibaren, ilan edilecek). **İlan edilince geriye say:** Faz 3 sunumdan **en az 7 gün**, Faz 5 **en az 5 gün** önce bitmeli.

| Faz | İş | Süre (TAHMİN) | Çıkış ölçütü |
|---|---|---|---|
| **0** | Okuma ve form | 6-10 saat | Spillway ve 4 kaynak okundu; fark cümlesi yazıldı; form gönderildi |
| **1** | Fizibilite pilotu | 8-12 saat | 5.000 cihaz kararlı bağlı; iki kesinti türü üretilebiliyor; ölçüm kaydı çalışıyor; sunucu seçildi |
| **2** | Simülatör ve ölçüm altyapısı | 25-40 saat | Tek çalıştırma uçtan uca; aynı tohumla benzer sonuç; hipotezler dosyada |
| **3** | **Çekirdek deney + analiz** | 15-22 saat aktif + ~23 saat makine (Kademe 1: ~16, Kademe 2: ~7) | Kademe 1 (140 çalıştırma) bitti; sessiz kesinti doğrulandıysa Kademe 2 (60) bitti; her hücre için medyan + güven aralığı + grafikler |
| **4** | Eklentiler (zaman kalırsa) | 0-25 saat | Aşağıya bak |
| **5** | Rapor, sunum, depo | 15-25 saat | Sunum ve rapor SABİS'e yüklenmeye hazır |

**Toplam: ~70-135 saat (TAHMİN).** Makine zamanı geceye yayılır.

### Faz 0 — Okuma ve form (19 Ekim'e kadar)

1. **Spillway** (SIGCOMM '26, 15b): özet, giriş, sınırlılıklar, ilgili çalışmalar. Amaç: "onlardan farkımız şu" cümlesi.
2. Domingues ve ark. 2024: "gelecek çalışma" cümlesini kendin gör.
3. RFC 6455 §7.2.3 ve RFC 5626 §4.5.
4. Isaacs HotOS'25, Huang OSDI'22: yalnızca giriş ve tartışma.
5. arXiv ve Scholar'da son tarama. **Gönderim gününde bir daha.**
6. Formu 14-17 Ekim'de gönder.

### Faz 1 — Fizibilite pilotu (form öncesi yapılması önerilir, zorunlu değil)

**Neden form öncesi?** Konu uygulanamazsa formu göndermeden anlarız. Sen "acele yok" dedin; bu seçenek sende.

Cevaplanacak sorular:
1. Docker'ın bellek sınırını artırınca (`.wslconfig`) kaç bağlantı rahat tutuluyor?
2. **Mosquitto mu, EMQX mi?** 5.000 bağlantıyı nasıl tutuyorlar (CPU, bellek)?
3. MQTTnet'le tek bir konteynerde 5.000 sanal cihaz bağlanıyor mu?
4. **TLS'siz bağlanma fırtınası çıkıyor mu, TLS'li çıkıyor mu?** (Fırtına hiç çıkmazsa tasarım değişir.)
5. İki kesinti türü üretilebiliyor mu ve etiketine uyuyor mu (tcpdump/ss)?
6. Sunucuya "ne kadar sonra gel" ipucunu verebilmek için hangi yol var (EMQX'in bağlantı hızı sınırı, sunucu eklentisi, önüne basit bir geçit)? Faz 4 için.

**Geri çekilme kuralları:**
- 5.000 tutmuyorsa N = 2.000 / 5.000 / 10.000.
- Sessiz kesinti gerçekten sessiz değilse (ya da üretilemezse) **Kademe 2 iptal edilir**, yalnızca temiz kesinti kalır. Bunu raporda yaz. Çekirdek zarar görmez.
- TLS'siz fırtına yoksa: TLS'li ya da yapay maliyetli kimlik doğrulama ile ilerle ve gerekçeyi yaz.

### Faz 2 — Simülatör ve ölçüm altyapısı

Bölüm 5'teki 1-7. Sıra: önce çalıştırıcı + toplayıcı (boş deney K2 çalışsın), sonra simülatör, sonra politikalar ve zamanlama.

### Faz 3 — Çekirdek deney (iki kademe)

Bölüm 4. Her blokta her hücre rastgele sırada bir kez. **Isıl daralma dışlama kuralı önceden yazılır** (Faz 2'de).

1. **Kademe 1:** Temiz kesinti. A × B × N, 120 çalıştırma + 20 kontrol. Önce bu yapılır ve analiz edilir.
2. **Kademe 2:** Sessiz kesinti, yalnızca N = 5.000, 60 çalıştırma. Kademe 1 bittikten sonra başlar. Böylece S4 ("kesinti türü sonucu değiştiriyor mu?") cevaplanır.

### Faz 4 — Eklentiler (sırayla, hepsi isteğe bağlı)

| Eklenti | İş | Ek çalıştırma (TAHMİN) |
|---|---|---|
| **E1** | **Sunucu hızlı kol (C):** sunucu yeniden bağlanma hızını yönetir. Standart ipuçları (0x89, 0x9F neden kodları) mı, kendi alanımız mı (CONNACK User Property + eşik hızı). Sınır, pahalı doğrulamadan **önce** reddetmeli | ~60 |
| **E2** | **Planlı kapatma (üçüncü kesinti türü):** sunucu önce cihazlara "gidiyorum" der. Açıklayıcı şekil. (Temiz ve sessiz kesinti artık Faz 3'te) | ~30 |
| **E3** | N = 30.000 | ~40 |
| **E4** | İkinci sunucuyla tekrar (dış geçerlilik) | ~40 |

### Faz 5 — Rapor, sunum, depo

Sunum iskeleti:
1. Problem (cihazlar kesintiden sonra aynı anda döner)
2. Bilinenler (RFC 6455, RFC 5390, Spillway, Domingues)
3. Boşluk: bağlantı sonrası yük ayrı ölçülmemiş (**bulamadık**)
4. Ortam (mimari şeması)
5. Deney tasarımı ve kontroller
6. Sonuçlar: zaman serisi, TTR, devrilme noktası
7. Tartışma: ne söyleyebiliriz, ne söyleyemeyiz (tek dizüstü, göreceli)
8. Sonuç ve tez bağlantısı

### Zaman yetmezse (küçültme sırası)

1. Faz 4'ü tamamen at.
2. **Kademe 2'yi (sessiz kesinti) at.** Yalnızca temiz kesinti kalır, S4 cevapsız kalır, bunu raporda yaz.
3. N = 15.000'i at, yalnızca 5.000.
4. Tekrar sayısını 10'dan 5'e indir (güven aralığı genişler, bunu yaz).
5. Politikayı A2'ye indir (A1 yalnızca K1'de kalsın).

**Hangisi olursa olsun proje tam sayılır;** çünkü çekirdek soru (S1-S3) hâlâ ölçülmüş olur.

### 19 Ekim'e kadar takvim

| Tarih | İş |
|---|---|
| 3 – 9 Ekim | Faz 0 okuma (Spillway öncelikli) |
| 6 – 12 Ekim | Faz 1 pilotu (isteğe bağlı, önerilir) |
| 12 – 14 Ekim | Son tarama, form metnini son hâle getir |
| **14 – 17 Ekim** | **Formu gönder** |
| **19 Ekim 17:30** | **Son tarih** |

---

## 7. Riskler ve karşılıkları

| # | Risk | Karşılık |
|---|---|---|
| 1 | **Fırtına hiç çıkmaz** (bağlanma ucuz, sunucu rahat) | Bağlantı maliyeti kalibrasyonu (TLS, doğrulama); N'yi artır; çıkmayan ölçekleri bulgu olarak yaz |
| 2 | **Dizüstü darboğaz olur** (yük üretici, Docker, ısı) | Boş deney K2 ve kalibrasyon; çekirdek bölüşümü sabit; priz + "En iyi performans"; ısıl dışlama kuralı (15a Ek) |
| 3 | **Kesinti etiketine uymaz** (örn. "sessiz" kesinti aslında temiz) | tcpdump ve `ss` ile doğrula; IP'yi sabitle. **Doğrulanamazsa Kademe 2 iptal** (Kademe 2'nin tamamı buna bağlı) |
| 4 | **H2 çürür** (ikinci dalga yok) | Rapor edilir; ortam ürün olarak kalır |
| 5 | **"Zaten yapılmış" itirazı** | Dürüst cümle; Spillway ve Domingues ile fark; "bulamadık" |
| 6 | **Sunucu hızlı kol teknik olarak zor** | Faz 4'e bırakıldı; Faz 1'de yol araştırılır; olmazsa dışarıda |
| 7 | **"Proje" beklentisi belirsiz** | Hem ürün hem rapor üretiyoruz. Yalnızca öneri beklenirse Faz 0-1 + bu dosya yeter |
| 8 | **Kapsam şişer** | Küçültme sırası (yukarıda); çekirdek bitmeden Faz 4'e geçme |
| 9 | **Kütüphane davranışı** (MQTTnet'in kendi yeniden bağlanması, bağlantı başına bellek) | Faz 1'de dene, otomatik yeniden bağlanmayı kapat |

---

## 8. Açık noktalar

| Konu | Durum |
|---|---|
| Sunum tarihi, süresi, şablonu | Bilinmiyor; ilan edilince plan geriye sayılır |
| "Proje" türünün tanımı | Bilinmiyor; plan ikisine de uyuyor |
| Cihaz türü | **Genel sanal cihaz** (sensör, tablet fark etmez). Sunumda bir örnek senaryo seçersin |
| Simülatör dili | **.NET önerisi.** Sen onaylarsın |
| Spillway okundu mu? | Bekleniyor |
| Sunucu seçimi (Mosquitto / EMQX) | Faz 1'de |
| Docker'ın çalıştığı, sürümü, Python varlığı | Senin makinende kontrol edilmedi |

---

## 9. Bağlantılı dosyalar

- `15a` — yöntem, dizüstü yapılabilirliği, kesinti yaratma, ölçüt tanımları. **Sonundaki "Ek" senin makinen.**
- `15b` — okuma rehberi (6 makale + Spillway), açık kalanlar O1-O5.
- `15c` — özgünlük açıları, standartlarda ne var, dürüstlük notları.
- `13` ve `ondeney\redis-istemcileri\` — **yedek** (Redis istemcileri). Bu projeyle ilgisiz, dokunma.
