# 09 — Kayıp araştırması: toplayıcıya ulaşmayan mesajlar

Tarih: 16 Ekim 2026
Durum: **Koşular başlıyor** (hipotezler sonuçlar görülmeden önce yazıldı)
Rapor bölümü: Bulgular (5.5-5.7 kayıp notları), Tartışma ve Sınırlar

## Sorun

Adım 5, 5b ve 5c'nin 39 koşusunun 6'sında üretilen mesajların küçük bir kısmı toplayıcıya ulaşmadı (tekrar hiçbirinde yok):

| Sürüm | Koşu | Kayıp (üretilen ≈) |
|---|---|---|
| v6 | rastgeleli / B1 / tohum 1 | 1 (539.622) |
| v6 | rastgeleli / B1 / tohum 3 | 1 (539.633) |
| v7 | üstel / B2 / tohum 3 | **91** (1.079.144) |
| v7 | rastgeleli / B2 / tohum 2 | **132** (1.079.081) |
| v7 | rastgeleli / B1 / tohum 3 | 1 |
| v8 | rastgeleli / B2 90 sn-500 ms / tohum 3 | 2 |

Cihaz tarafında üretilen = canlı gönderilen + biriken, yani mesaj cihazdan çıkmış (ya da çıkmış sayılmış), toplayıcıda yok. Kayıp "QoS 1 ile teslim garantisi" varsayımının nerede kırıldığını gösterir; tıbbi bir bağlamda önemlidir.

## Aday açıklamalar

| # | Açıklama | Mekanizma |
|---|---|---|
| A | **Sunucu durdurulurken** (Mosquitto kalıcılığı kapalı) cihazlardan alınıp onaylanmış ama toplayıcıya henüz iletilmemiş mesajlar sunucuyla birlikte kaybolur | Kayıp mesajların planlanan zamanı durdurma anının hemen öncesindedir |
| B | **Sunucu açıldıktan sonra toplayıcı yeniden abone olana kadar** (yaklaşık 0,5 sn) cihazlardan gelen mesajlar için abone yoktur; Mosquitto bu mesajları (QoS 1'de bile) onaylayıp düşürür | Kayıp mesajlar sunucunun açıldığı an ile toplayıcının yeniden abone olduğu an arasında gönderilmiştir |
| C | Başka bir neden (toplayıcının kendi kuyruğu, cihaz tarafı hata, koşunun sonu) | A ve B'ye uymayan kayıplar |

## Hipotezler (koşulardan önce yazıldı)

| # | Hipotez | Not |
|---|---|---|
| HK1 | Kayıp mesajların gönderim zamanı **B** penceresindedir (sunucu açılışı ile toplayıcının yeniden abone oluşu arasında) | **Çürüyebilir** |
| HK2 | Kayıp mesajların planlanan zamanı **A** penceresindedir (durdurmadan önceki ~2 sn) | **Çürüyebilir** |
| HK3 | Kayıp, tohuma bağlıdır: v6'da kayıp görülen tohumlar (1 ve 3) tekrar koşulduğunda yine kayıp verir (cihaz zamanlaması tohumdan türetildiği için) | **Çürüyebilir**; tohum etkisini ayırır |

## Yöntem

- **Ham kayıt:** toplayıcı her mesajı (alınma anı, cihaz, mesaj numarası, tür, planlanan an, biriken mi) dosyaya yazar (`RAW_PATH`, `adim5.ps1 -HamKayit`). Dosyalar büyük olduğu için git dışıdır (`*.ham.csv`).
- **Bağlantı olayları** mono saatle damgalanır (toplayıcının kopma ve yeniden abone olma anları): `baglanti_olaylari` içinde `mono_ms=`.
- **Kayıp mesajın bulunması:** her cihazın mesaj numarası 1'den başlayıp birer artar; alınan numaralardaki boşluklar kayıp mesajlardır. Son numaradan sonraki kayıplar boşlukla görülemez; `üretilen - alınan` farkından bulunur ("kuyruk kaybı").
- **Zaman tahmini:** kayıp mesajın iki komşusunun planlanan ve alınma anları, sunucu durdurulmasından beri geçen saniye cinsinden yazılır (`scripts/kayip_analizi.py`).
- **Koşular:** rastgeleli A + B1, N = 5.000 (v6'da en sık kayıp veren hücre: 3 koşunun 2'sinde), **6 tekrar** (tohum 1-6), sürüm `k1`, ham kayıt açık.

| Öğe | Değer |
|---|---|
| Komut | `./scripts/matris5.ps1 -Tekrar 6 -Cihazlar 5000 -Politikalar ustel-rastgele -BPolitikalari b1 -HamKayit -Surum k1` |
| Analiz | `python scripts/kayip_analizi.py k1` ve `python scripts/analiz_adim5.py k1` |
| Süre | 6 koşu, ≈ 25 dk |

## Sonuçlar

`[ ]` Koşular bitince doldurulacak.

## Sınırlar (önceden bilinen)

- Yalnızca bir hücre incelenir; v7'deki büyük kayıplar (91, 132) N = 10.000 + B2'de görüldü ve burada yeniden üretilmeyebilir.
- Kayıp olayı nadir (bu hücrede v6'da 3 koşunun 2'sinde, her biri 1 mesaj); 6 koşuda hiç kayıp çıkmayabilir.
- Zaman tahmini komşu mesajlardan yapılır, kayıp mesajın tam gönderim anı bilinmez.
