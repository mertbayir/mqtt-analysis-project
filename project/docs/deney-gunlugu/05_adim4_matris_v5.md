# 05 — Adım 4: Tekrarlı deney matrisi (IP ile, 3 bekleme kuralı, N = 2.000 ve 5.000)

Tarih: 15 Ekim 2026
Durum: **Planlandı, koşu başlıyor** (sonuçlar henüz yok)
Rapor bölümü: Bulgular (5.1, 5.2, 5.3)

## Amaç

Üç yeniden bağlanma kuralını (sabit, üstel, rastgeleli üstel) aynı kesinti senaryosunda, **tekrarlı** koşularla karşılaştırmak. Önceki tek koşuluk sonuçlar (günlük 03 ve 04) ön gözlemdi; bu matris nihai bulgu içindir. Cihazlar sunucuya IP ile bağlanır (DNS artefaktı elendi, günlük 04 Bulgu 5).

## Hipotezler (sonuçlar görülmeden önce yazıldı, 15 Ekim 2026)

| # | Hipotez | Türü |
|---|---|---|
| H1 | Rastgeleli üstel kural, rastgelesiz kurallara göre kesintideki tepe bağlantı denemesi/sn değerini düşürür | Bilinen sonuç ([1], [2]); ortamın doğru çalıştığının sağlaması |
| H2 | Rastgeleli üstel kural, sunucu açıldıktan sonra toparlanma süresini (TTR) uzatır (izdihamı azaltmanın bedeli) | Bilinen eğilim; bu ortamda ölçülecek |
| H3 | Bu ortamda (TLS'siz Mosquitto, 2 CPU, N ≤ 5.000) sunucu zorlanmaz: sunucu CPU'su sınırın çok altında kalır ve sunucu açıkken başarısız bağlantı çok az olur | **Çürüyebilir.** Çürürse "fırtına" bulgusudur |
| H4 | N = 2.000 ile 5.000 arasında TTR farkı küçüktür (devrilme noktası bu aralıkta yok) | **Çürüyebilir** |

Hipotezler değişirse bu tablo silinmez, karar defterine tarihle yazılır.

## Tasarım

| Öğe | Değer |
|---|---|
| Faktörler | Kural (3: sabit, üstel, rastgeleli üstel) × N (2: 2.000, 5.000) = 6 hücre |
| Tekrar | 3 blok (tohum 1, 2, 3). Her blokta 6 hücre **rastgele sırayla** bir kez |
| Toplam | 18 koşu, tahmini süre ~65 dk |
| Kesinti | Temiz (`docker stop`), 60 sn kapalı, ardından 90 sn toparlanma izleme |
| Başlangıç | Cihazlar 20 sn'ye yayılarak bağlanır; kesinti t=40 sn'de |
| Sunucu | Mosquitto 2.1.2, 2 CPU / 2 GB, TLS yok, IP ile erişim, sabit MAC |
| Simülatör | 4 CPU / 3 GB, Docker içinde, kütüphane otomatik yeniden bağlanma kapalı |
| Sürüm etiketi | `v5` (koşu adı: `adim3_<kural>_n<N>_s<tohum>_v5`) |
| Betik | `scripts/matris.ps1`; sıra ve ilerleme `results/matris_*.txt` |
| Özet | `scripts/ozet.ps1 -Onek adim3` ve (yazılacak) tekrar özeti |

## Analiz planı (önceden)

- Hücre başına 3 koşu: **medyan ve aralık (en küçük-en büyük)** verilir. n = 3 ile güven aralığı iddiası yapılmaz; yalnızca tüm koşuların dağılımı gösterilir.
- Ölçütler: TTR %50/95/100, kesintideki tepe deneme/sn, toplam deneme, sunucu açıkken başarısız bağlantı, sunucu ve simülatör CPU tepesi.
- Tek bir koşu "aykırı" görünse bile **çıkarılmaz**, raporlanır.
- Bir koşu teknik olarak başarısızsa (betik hatası, sunucu açılmadı) geçersiz sayılır ve **nedeniyle** işaretlenir.

## Sonuçlar

`[ ]` Koşular bitince doldurulacak.

## Yorum, sınırlar

`[ ]`

## Dosyalar

`results/adim3_*_v5.*`, `results/matris_*.txt`, `results/ozet_adim4.txt`
