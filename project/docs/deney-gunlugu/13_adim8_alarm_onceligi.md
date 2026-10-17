# 13 — Adım 8: Alarm önceliği (B3) ve birikmiş alarmın yaşı

Tarih: 17 Ekim 2026
Durum: **Kod yazıldı (`c54fe16`), çalışma testi geçti, matrisler başlıyor.** Hipotezler sonuçlardan önce yazıldı; çalışma testi yazım sırasında biliniyordu, bkz. "Çalışma testi". (Kod, EMQX matrisleri bittikten sonra yazıldı: koşular sırasında `src/` ve `scripts/` değiştirilmez, çünkü her koşu imajı o anki koddan derler.)
Rapor bölümü: Bulgular (5.10 birikmiş alarmın yaşı, 5.11 B3), Tartışma
Önceki adımlar: 06-08 (B0/B1/B2), 11 (TLS)

## Amaç

Şu ana kadar yalnızca **canlı** alarmın gecikmesine baktık. Ama klinik açıdan iki ayrı soru var:
1. **Canlı alarm** ne kadar gecikiyor? (şimdiye kadarki ölçüt)
2. **Kesintide üretilmiş (birikmiş) alarm** sunucuya ne kadar geç ulaşıyor? Hasta başı cihaz kesinti sırasında bir alarm üretmiş olabilir; B2 gibi boşaltmayı yayan politikalar canlı alarmı korurken **birikmiş alarmı daha da geciktirir**. B1/B2'de alarm da vital mesajlarla aynı sırada bekliyor.

Önerilen çözüm: **B3 (alarm öncelikli)**: bağlanır bağlanmaz **önce birikmiş alarmlar** (az sayıda) hemen gönderilir; ardından geri kalan (vital) mesajlar B2 gibi rastgele gecikmeyle ve aralıklı boşaltılır. Soru: B3 birikmiş alarmın yaşını düşürürken canlı alarm gecikmesini bozuyor mu?

## Mevcut verilerden ilk bakış (yeni koşu yok; kaynak: toplayıcının `biriken_alarm` sınıfı)

"Yaş": biriken alarmın planlanan üretim anı ile toplayıcıya ulaştığı an arasındaki süre (saniye; 3 koşunun medyanı).

| Koşul | A / B | Yaş p50 | Yaş p95 | Yaş en büyük |
|---|---|---|---|---|
| TLS'siz, N = 5.000 (v6) | üstel / B1 | 55,6 | 98,5 | 103,8 |
| | üstel / B2 30-200 | 64,1 | 110,5 | 116,3 |
| | rastgeleli / B1 | 36,6 | 78,1 | 88,8 |
| | rastgeleli / B2 30-200 | 56,0 | 99,8 | 114,7 |
| TLS, N = 5.000 (tls6) | üstel / B1 | 59,2 | 123,5 | 130,4 |
| | üstel / B2 | 68,0 | 139,6 | 152,3 |
| | rastgeleli / B1 | 38,4 | 79,2 | 90,5 |
| | rastgeleli / B2 | 56,4 | 99,7 | 118,3 |
| TLS'siz, N = 10.000 (v7, v8) | rastgeleli / B1 | 40,9 | 89,9 | 96,1 |
| | rastgeleli / B2 30-200 | 65,0 | 113,5 | 122,8 |
| | rastgeleli / B2 60-1000 | 90,2 | 129,1 | 145,1 |
| | rastgeleli / B2 90-500 | 93,9 | 157,8 | 173,4 |

Okuma: B2 canlı alarmı korurken birikmiş alarmı yaklaşık 20-50 sn (p50) geciktirir; N = 10.000'deki uzun pencereli B2'de en eski birikmiş alarm kesinti bittikten sonra bile yaklaşık 3 dakikalık yaşta ulaşır (kesintinin kendisi 60 sn). Bu bedel makalede şimdiye kadar yazılmadı. (Hesap betiği: koşular bittikten sonra `scripts/` altına eklenecek; şimdilik geçici betik. Not: p50 ve p95 saniye başına yüzdeliklerin adet ağırlıklı hesabıdır, yaklaşık değerdir.)

## B3 tasarımı

| Öğe | Değer |
|---|---|
| Politika | B3 = bağlanır bağlanmaz birikmiş **alarmlar peş peşe** gönderilir (gecikme yok, eşitlemeden önce); sonra B2 gibi: U(0, pencere) gecikme, eşitleme, kalan (vital) mesajlar aralıklı |
| Parametre | Pencere ve aralık B2 ile aynı (`B2_WINDOW_SEC`, `B2_INTERVAL_MS`); etiket `b3` (varsayılan 30/200) ya da `b3-w90i500` |
| Yeni ölçüt | Biriken alarmın yaşı (p50, p95, en büyük); `biriken_alarm` sınıfından, `analiz_adim5.py`'ye eklenecek |
| Değişen dosyalar | `src/Simulator/PostConnectPolicy.cs` (`B3AlarmOnce`, arayüze `AlarmOnce`), `Device.cs` (`BaglantiSonrasiAsync` başında alarm-önce boşaltma; olay `alarm_flush_bitti`), `Program.cs` (`POLICY_B=b3`), `adim5.ps1` / `matris5.ps1` (b3 ve `b3-w..i..` etiketi), `analiz_adim5.py` (birikmiş alarmın yaşı, `alarm_flush_bitis_max_sn`). Web arayüzü (`web_uret.py`, `sablon.html`) koşular bitince güncellenir |

## Çalışma testi (nihai veri değil)

`adim5_ustel-rastgele_b3_n1000_s1_b3test` (N = 1.000, rastgeleli A, B3 30 sn / 200 ms, tek koşu, TLS'siz, izleme 120 sn): B3 tasarlandığı gibi çalıştı. Tüm cihazlar `alarm_flush_bitti` (1.000) ve `flush_bitti` (1.000) olayı yazdı; biriken alarm 7.281 adet; **alarm boşaltması en geç sunucu açılışından 30,1 sn sonra bitti** (yeniden bağlanma süresiyle sınırlı), vital boşaltma 62,3 sn'de; biriken alarmın yaşı p50 36,9 / p95 77,8 / en büyük 85,4 sn; canlı alarm tepe p99 24 ms; kayıp 0, tekrar 0. Analiz betiğindeki yeni ölçütler v6 verisinde önceki hesapla birebir aynı çıktı.

## Hipotezler (matris öncesi yazıldı)

| # | Hipotez | Not |
|---|---|---|
| HP1 | **B3'te biriken alarmın yaş medyanı**, aynı pencere/aralıklı B2'ninkinden en az %25 düşüktür | **Çürüyebilir**; alt sınır, yeniden bağlanma süresi |
| HP2 | **B3'te biriken alarmın en büyük yaşı**, B2'ninkinden en az %25 düşüktür | **Çürüyebilir** |
| HP3 | **B3'te canlı alarm p99 tepesi**, B2'ninkinin en çok 2 katıdır (alarmları öne almak canlı alarmı bozmaz; birikmiş alarm sayısı toplamın ≈ %7'si) | **Çürüyebilir** |
| HP4 | **B3'te boşaltma bitişi**, B2'ninkinden en çok %10 farklıdır | **Çürüyebilir** |
| HP5 | Tekrar 0, kayıp ≤ 1 | **Çürüyebilir** |

## Tasarım

| Matris | Koşul | Hücreler | Tekrar | Koşu | Sürüm |
|---|---|---|---|---|---|
| **P1** | Mosquitto, TLS'siz, N = 10.000, rastgeleli A, izleme 180 sn | B1, B2 (90 sn / 500 ms), B3 (90 sn / 500 ms) | 3 blok | 9 | `p1` |
| **P2** | Mosquitto, TLS, N = 5.000, izleme 180 sn | A (üstel, rastgeleli) × B2 (30/200), B3 (30/200) | 3 blok | 12 | `p2` |

(Her matriste B2 hücresi aynı blokta yeniden koşulur; önceki sürümlerle çapraz blok karşılaştırması yapılmaz. P2'de B1 yok: B1 için tls6 var, ama aynı blokta olmadığı belirtilerek ikincil karşılaştırmada kullanılır.)

## Sonuçlar

`[ ]` Kod yazılıp matrisler koşulunca doldurulacak.

## Sınırlar (önceden bilinen)

- Birikmiş alarm sayısı küçük (cihaz başına ≈ 6-7); B3'ün kazancı yeniden bağlanma süresiyle sınırlıdır (alarm, cihaz geri dönmeden gönderilemez).
- Alarm/vital ayrımı varsayımsaldır; gerçek bir cihazda alarm önceliği ve kuyruk yönetimi (ör. MQTT 5 mesaj önceliği yok) farklı olabilir.
- "Yaş" ölçütü, alarmın planlanan üretim anından ölçülür (cihaz saati ile toplayıcı saati aynı çekirdek saati).
- 06 ve 07'nin tüm sınırları geçerli.
