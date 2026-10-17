# 12 — Adım 7: İkinci sunucu (EMQX, çok çekirdekli)

Tarih: 17 Ekim 2026
Durum: **Matrisler başlıyor** (hipotezler sonuçlar görülmeden önce yazıldı; geçerli bir EMQX pilotunun sonucu yazım sırasında biliniyordu, bkz. "Pilotlar")
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

`[ ]` Matrisler bitince doldurulacak.

## Sınırlar (önceden bilinen)

- **Yalnızca iki sunucu**, ikisi de tek makinede, aynı Docker ağında; EMQX tek düğüm (kümeleme yok).
- EMQX varsayılandan farklı yapılandırıldı (`force_shutdown` kapalı, kuyruk sınırsız); başka varsayılanlar (ör. `max_conn_rate`, `overload_protection` kapalı) incelenmedi.
- "Aynı kaynak" = aynı CPU sınırı; sunucuların çekirdek başına verimi farklı olduğundan çekirdek sayısı tek başına adalet ölçütü değildir. E2 bu yüzden eklendi.
- EMQX'in `docker stop` ile kapanışı (2,4 sn) ve açılışı (0,9 sn) Mosquitto'dan farklıdır; kesinti süresi aynı (60 sn) ama sunucunun açılış davranışı karşılaştırmaya karışır.
- Aynı blokta koşulmayan karşılaştırma (Mosquitto v6, EMQX e1).
- Mosquitto'nun TLS ve N = 10.000 hücreleri EMQX ile bu adımda karşılaştırılmadı.
