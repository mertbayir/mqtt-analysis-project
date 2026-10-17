# 12 — Adım 7: İkinci sunucu (EMQX, çok çekirdekli)

Tarih: 17 Ekim 2026
Durum: **Matrisler tamamlandı (E1 18/18, E2 12/12 koşu)**. Hipotezler sonuçlar görülmeden önce yazıldı; geçerli bir EMQX pilotunun sonucu yazım sırasında biliniyordu, bkz. "Pilotlar")
Rapor bölümü: Yöntem (3.1 sunucular), Bulgular (5.9 ikinci sunucu), Tartışma, Sınırlar
Önceki adımlar: 06-08 (Mosquitto, TLS'siz), 10-11 (Mosquitto, TLS)

## Amaç

Şimdiye kadarki tüm bulgular tek bir sunucuya (Mosquitto 2.1.2, **tek iş parçacıklı**) dayanıyor. Soru: "ikinci izdiham" ve politikaların sıralaması **tek çekirdeğe özgü mü**, yoksa çok çekirdekli bir sunucuda da var mı? Ve çekirdek sayısı artınca etki azalıyor mu?

## Kurulum

| Öğe | Değer |
|---|---|
| Sunucu | **EMQX 5.8.6** (resmî `emqx/emqx:5.8` imajı, sabitlenmiş imza `sha256:a1e3d10f...`), çok iş parçacıklı (Erlang) |
| Kaynak | Mosquitto ile aynı pay: **2 CPU / 2 GB** (konteyner sınırı); EMQX 2 çalışma iş parçacığı açıyor (`schedulers_online = 2`, doğrulandı). Ayrı bir matriste 4 CPU |
| Ağ | Aynı sabit MAC, aynı adres (172.21.0.2), TLS'siz 1883 (TLS'li 8883 hazır, bu adımda kullanılmadı), anonim erişim |
| Kuyruk/uçuştaki sınırlar | `max_mqueue_len = infinity`, `max_inflight = 100` (Mosquitto'daki `max_queued_messages 0`, `max_inflight_messages 100` eşdeğeri) |
| **Ölçüm için değiştirilen koruma** | **`force_shutdown` kapalı.** Varsayılan ayarda (posta kutusu sınırı 1.000 mesaj) EMQX, boşaltma sırasında tek toplayıcı aboneyi `mailbox_overflow` ile defalarca bağlantıdan attı (pilot 2); toplayıcı atılınca mesajlar ve eşitleme yanıtları kayboldu. Bu ayar Mosquitto'da kuyruk sınırının kaldırılmasına eşdeğer bir ölçüm ayarıdır; aynı zamanda **bir bulgudur**: varsayılan EMQX ayarıyla tek yoğun abone boşaltma anında koparılıyor |
| Diğer | WebSocket dinleyicileri ve panel kapalı; konsol günlük düzeyi `warning`; başka ayar varsayılan |
| Betik | `scripts/adim5.ps1 -Sunucu emqx [-SunucuCpu 4]` (aynı iş yükü, aynı toplayıcı, aynı analiz); compose: `docker-compose.emqx.yml` |
| Sunucu açılış süresi | `docker start`tan yayımlanan 1883 portu (127.0.0.1) açılana kadar geçen süre ölçülüp `olaylar.txt` içine yazılır (pilot 3: 0,9 sn; Mosquitto ≈ 0,5 sn) |
| Durdurma süresi | `docker stop` EMQX'te 2,4 sn sürdü (düzgün kapanış; Mosquitto ≈ 0,1 sn). Hizalama duyarlılığı denendi: sunucu açılış anı ±2,4 sn kaydırılınca pilot 3'ün tepe ve eşik-üstü değerleri değişmedi (12.624 ms, 29 sn) |

## Pilotlar (nihai veri değil)

| Pilot | Sonuç |
|---|---|
| `emqxpilot` | **Geçersiz:** bir istemci hatası (TLS'siz modda TLS seçeneği açık kalıyordu; kod değişikliği TLS adımında yapılmıştı, hiçbir TLS'siz koşu o günden sonra yapılmadığı için fark edilmedi) yüzünden hiçbir cihaz bağlanamadı. Hata düzeltildi. Etkilenen koşu yok: Adım 1-5c TLS kodundan önce koşuldu; Adım 6 ve 6b zaten TLS'liydi |
| `emqxpilot2` | **Geçersiz:** `force_shutdown` toplayıcıyı atıyordu; kayıp 272.526 mesaj, 4.636 eşitleme hatası. Ayar kapatıldı |
| `emqxpilot3` | **Geçerli:** EMQX (2 CPU), üstel + B1, N = 5.000, tek koşu: alarm p99 tepe **12.624 ms** (taban 33 ms), 29 sn > 100 ms, eşitleme p99 9.924 ms (982 eşitleme zaman aşımı), sunucu CPU tepe **204 / 200**, boşaltma bitişi 45,6 sn, kayıp 0, tekrar 0. Yani aynı hücrede Mosquitto'dan (9.158 ms) daha kötü |

## Hipotezler (matris öncesi yazıldı)

| # | Hipotez | Not |
|---|---|---|
| HE1 | **EMQX (2 CPU), üstel + B1, N = 5.000:** alarm p99 tepesi tabanın en az 10 katıdır ("ikinci izdiham" çok çekirdekli sunucuda da var) | Pilotta gözlendi (≈ 380 kat); tekrarlı doğrulama |
| HE2 | **EMQX (2 CPU), B2 (30 sn / 200 ms), B1'e göre** tepeyi en az %50 düşürür (her iki A için) | **Çürüyebilir** |
| HE3 | **EMQX (2 CPU), rastgeleli + B1:** tepe tabanın en az 5 katıdır (Mosquitto'da ≈ 1,7 kattı; EMQX'in çekirdek başına işi daha yavaş, 2 çekirdek yeterince pay bırakmıyor olabilir) | **Çürüyebilir** |
| HE4 | **EMQX (2 CPU), B0:** tepe 100 ms'nin altında kalır (iki A için) | **Çürüyebilir** |
| HE5 | **EMQX 4 CPU, üstel + B1**, 2 CPU'ya göre tepeyi en az %50 düşürür (çekirdek eklemek yardım eder) | **Çürüyebilir** |
| HE6 | Tüm koşularda tekrar = 0 ve kayıp ≤ 1 mesaj | **Çürüyebilir** |

## Tasarım

| Matris | Hücreler | Tekrar | Koşu | Sürüm | Komut |
|---|---|---|---|---|---|
| **E1** (EMQX 2 CPU) | A (üstel, rastgeleli) × B (b0, b1, b2) | 3 blok | 18 | `e1` | `./scripts/matris5.ps1 -Tekrar 3 -Cihazlar 5000 -Sunucu emqx -SonrasiSn 120 -Surum e1 -Not "Adim 7 EMQX 2 CPU"` |
| **E2** (EMQX 4 CPU) | A (üstel, rastgeleli) × B (b1, b2) | 3 blok | 12 | `e2` | `./scripts/matris5.ps1 -Tekrar 3 -Cihazlar 5000 -Sunucu emqx -SunucuCpu 4 -Politikalar ustel,ustel-rastgele -BPolitikalari b1,b2 -SonrasiSn 120 -Surum e2 -Not "Adim 7 EMQX 4 CPU"` |

N = 5.000, TLS'siz, aynı zaman çizelgesi (yayılım 20 sn, kesinti t = 40 sn'de 60 sn), izleme 120 sn. Karşılaştırma: Mosquitto v6 (aynı hücreler, aynı N). Analiz: `python scripts/analiz_adim5.py e1` ve `e2`. Süre: E1 ≈ 95 dk, E2 ≈ 63 dk.

## Sonuçlar

### Koşu akışı (kayıt)

E1 `results/matris5_20261017_1515.txt` (18 koşu), E2 `results/matris5_20261017_1635.txt` (12 koşu); 0 betik hatası, kesinti yok. Kayıt defterinde 30 satır (`_e1`, `_e2`). Kod bayrağı: bir E1 koşusu (`adim5_ustel-rastgele_b2_n5000_s1_e1`) `kod_degisikligi_var=True` taşır; neden, koşu sırasında `scripts/web_uret.py` dosyasının (web arayüzü veri üreticisi, deneyle ilgisi yok) depoya eklenmesiydi. Doğrulandı: `9957777` (matris başlangıcı) ile `551df0b` arasında `src/`, `scripts/`, compose ve `mosquitto/` altında tek fark o dosyadır (`git diff --stat`). Kalan 29 koşu temiz.

### E1: EMQX 2 CPU, N = 5.000, TLS'siz (medyan [en küçük-en büyük]; parantez: Mosquitto v6 medyanı)

Taban 28-43 ms.

| A | B | Alarm p99 tepe (ms) | >100 ms süre (sn) | Boşaltma bitişi (sn) | Sunucu CPU tepe (100 = 1 çekirdek) |
|---|---|---|---|---|---|
| üstel | B0 | 112 [105-141] (47) | 1 [1-2] (0) | - | 149 [132-185] (37) |
| üstel | B1 | **16.188** [12.158-16.266] (9.158) | 30 [28-32] (19) | 48,8 [42,7-49,6] (42,6) | 207 [207-210] (98) |
| üstel | B2 | 186 [184-205] (143) | 19 [18-24] (17) | 58,6 [58,6-58,8] (65,6) | 157 [155-183] (58) |
| rastgeleli | B0 | 66 [59-112] (60) | 0 [0-1] (0) | - | 132 [132-138] (21) |
| rastgeleli | B1 | 93 [92-156] (51) | 0 [0-5] (0) | 32,0 [31,8-32,1] (28,9) | 200 [196-203] (85) |
| rastgeleli | B2 | 163 [157-309] (79) | 3 [2-7] (0) | 71,0 [69,0-73,5] (63,7) | 155 [154-198] (59) |

Eşitleme p99 (üstel / B1): 9.801 ms [9.704-9.974], 687-1.649 eşitleme zaman aşımı (Mosquitto: 8.147 ms, 0-50). Tekrar 0 (18 koşu).

### E2: EMQX 4 CPU, N = 5.000, TLS'siz (medyan [en küçük-en büyük])

Taban 32-58 ms.

| A | B | Alarm p99 tepe (ms) | >100 ms süre (sn) | Boşaltma bitişi (sn) | Sunucu CPU tepe (400 = tüm pay) |
|---|---|---|---|---|---|
| üstel | B1 | **11.527** [11.374-22.805] | 25 [23-47] | 39,0 [37,9-59,5] | 389 [388-399] |
| üstel | B2 | 161 [122-516] | 17 [5-24] | 59,0 [58,1-63,5] | 332 [238-378] |
| rastgeleli | B1 | 127 [92-211] | 2 [0-3] | 32,1 [31,9-33,2] | 345 [312-352] |
| rastgeleli | B2 | 316 [75-6.576] | 2 [0-38] | 71,3 [68,5-71,5] | 278 [273-415] |

Tekrar 0 (12 koşu). **Aykırı koşular çıkarılmadı:** üstel/B1 tohum 2 (22.805 ms, 47 sn) ve rastgeleli/B2 tohum 2 (6.576 ms, 38 sn; öbür ikisi 75 ve 253 ms).

### Sunucu açılış / kapanış (EMQX, tüm koşular)

`docker start`tan portun açılmasına 0,68-0,95 sn (Mosquitto ≈ 0,5 sn); `docker stop` 2,4-2,6 sn. Hizalama duyarlılığı (±2,4 sn) tepe ve eşik-üstü değerlerini değiştirmedi (pilot 3'te denendi).

### Hipotezlerin durumu

| # | Sonuç | Kanıt |
|---|---|---|
| HE1 | **Destekleniyor** | EMQX 2 CPU, üstel + B1: tepe 16.188 ms, tabanın ≈ 540 katı (Mosquitto 9.158 ms) |
| HE2 | **Kısmen** | Üstelde B2, B1'e göre %99 düşürdü (16.188 → 186 ms). Rastgelelide B2 B1'den **yüksek** (163 vs 93 ms); "iki A için" koşulu sağlanmadı |
| HE3 | **Çürüdü** | Rastgeleli + B1 tepe 92-156 ms, kendi koşularının tabanının 2,8-3,6 katı (< 5) |
| HE4 | **Çürüdü** | B0 tepesi üstelde 105-141 ms (≥ 100); rastgelelide 59-112 |
| HE5 | **Çürüdü** | 4 CPU üstel + B1: 11.527 ms, 2 CPU'ya göre yalnızca %29 düşük (< %50); sunucu CPU tepesi 389 / 400 (4 çekirdek de doluyor) |
| HE6 | **Kısmen çürüdü** | Tekrar 30 koşuda da 0. Kayıp ≤ 1 koşulu 30 koşunun 17'sinde aşıldı (E1: 14/18, en çok 25; E2: 3/12, en çok 69; toplam üretilenin ≤ %0,011'i) |

### Gözlemler

1. **"İkinci izdiham" çok çekirdekli sunucuda da var.** Aynı kaynak payında (2 CPU) EMQX'te üstel + B1'de canlı alarm gecikmesi tepesi 16 sn; Mosquitto'dan (9 sn) kötü. Çekirdek eklemek (4 CPU) tepeyi 11,5 sn'ye indirdi (%29), sunucu yine doldu (CPU 389/400).
2. **Aynı CPU payında EMQX her hücrede Mosquitto'dan yavaş** (tepe 1,1-2,4 kat: üstel B0 47 → 112, üstel B1 9.158 → 16.188, üstel B2 143 → 186, rastgeleli B0 60 → 66, B1 51 → 93, B2 79 → 163). Çekirdek başına verim Mosquitto'da daha yüksek: yalnızca bağlanma yükünde (B0, üstel) EMQX 149 / 200, Mosquitto 37 / 100 CPU kullanıyor. Bu, EMQX'in başka özelliklerinin (kalıcı oturum, kural motoru, ölçüm) yükünü de içerebilir; ayrıştırılmadı.
3. **Politika sıralaması iki sunucuda da aynı:** B1 kilit adım dönüşte (üstel) felaket; B2 üstelde gecikmeyi %98-99 düşürüyor; rastgele dönüş B1'i zaten kurtarıyor ve B2 orada B1'den kötü; B0 tabana yakın. Bulgular tek çekirdeğe özgü değil; büyüklükler sunucuya bağlı.
4. **Çekirdek eklemek yeterli değil:** 4 CPU'da rastgeleli kural ve B2 hücrelerinde iyileşme yok (127 ve 316 ms; ikisi aykırı koşu içeriyor). 4 CPU + simülatör (4) + toplayıcı (2) = host'un 12 çekirdeğinin 10'u; host'un doyması ölçümü etkilemiş olabilir (E2 sınırı).
5. **EMQX varsayılan ayarı ölçüm düzeneğiyle çakıştı:** `force_shutdown` (posta kutusu 1.000) tek yoğun aboneyi boşaltma sırasında koparıyordu (pilot 2). Gerçek dağıtımda yoğun bir toplayıcı/abone aynı ayarla koparılır; kapatma bu çalışmanın ölçüm ayarıdır.
6. **Kayıp EMQX'te daha sık** (17/30 koşuda > 1 mesaj; Mosquitto'da 39 koşunun 3'ünde), ama oransal olarak küçük (≤ %0,011). Nedeni açıklanmadı (ham kayıt EMQX'te alınmadı).

### Sınırlar ve açık noktalar

- İki sunucu, tek makine; EMQX tek düğüm, varsayılandan farklı (`force_shutdown` kapalı, kuyruk sınırsız), diğer varsayılanlar incelenmedi.
- Aynı CPU payı adil karşılaştırma değildir (sunucuların çekirdek başına verimi farklı); E2 bunun için eklendi ama host doyması karışıyor.
- Karşılaştırma değerleri (Mosquitto v6) aynı blokta koşulmadı.
- E2'de iki aykırı koşu (22.805 ve 6.576 ms) çıkarılmadı; nedenleri incelenmedi.
- TLS ve N = 10.000 EMQX'te denenmedi.
- Kayıp mekanizması açıklanmadı; ham kayıt (EMQX) alınmadı.

## Sınırlar (önceden bilinen)

- **Yalnızca iki sunucu**, ikisi de tek makinede, aynı Docker ağında; EMQX tek düğüm (kümeleme yok).
- EMQX varsayılandan farklı yapılandırıldı (`force_shutdown` kapalı, kuyruk sınırsız); başka varsayılanlar (ör. `max_conn_rate`, `overload_protection` kapalı) incelenmedi.
- "Aynı kaynak" = aynı CPU sınırı; sunucuların çekirdek başına verimi farklı olduğundan çekirdek sayısı tek başına adalet ölçütü değildir. E2 bu yüzden eklendi.
- EMQX'in `docker stop` ile kapanışı (2,4 sn) ve açılışı (0,9 sn) Mosquitto'dan farklıdır; kesinti süresi aynı (60 sn) ama sunucunun açılış davranışı karşılaştırmaya karışır.
- Aynı blokta koşulmayan karşılaştırma (Mosquitto v6, EMQX e1).
- Mosquitto'nun TLS ve N = 10.000 hücreleri EMQX ile bu adımda karşılaştırılmadı.
