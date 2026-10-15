# 05 — Adım 4: Tekrarlı deney matrisi (IP ile, 3 bekleme kuralı, N = 2.000 ve 5.000)

Tarih: 15 Ekim 2026
Durum: **Tamamlandı** (18/18 koşu, hata yok, kod `8a66190`, commit edilmemiş değişiklik yok)
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

## Sonuçlar (15 Ekim 2026)

Her hücre 3 koşu (tohum 1, 2, 3). Değerler **medyan [en küçük - en büyük]**. Koşu başına değerler: `results/ozet_adim4_kosular.csv`, hücre özeti: `ozet_adim4_hucreler.csv`, ekran çıktısı `ozet_adim4.txt` (`scripts/tekrar_ozet.py v5`).

| N | Kural | Kesintide tepe deneme/sn | TTR %50 (sn) | TTR %100 (sn) | Sunucu açıkken başarısız bağlantı | Sunucu CPU tepe (%, sınır 200) |
|---|---|---|---|---|---|---|
| 2.000 | sabit | 2.000 [2.000-2.000] | 5,3 [5,0-5,3] | 6,3 [6,0-6,3] | 2.065 [2.052-2.169] | 9,9 [7,7-10,8] |
| 2.000 | üstel | 2.000 [2.000-2.000] | 24,2 [24,1-24,4] | 25,2 [25,1-25,4] | 0 [0-0] | 3,3 [2,1-11,5] |
| 2.000 | rastgeleli üstel | 484 [475-521] | 12,3 [12,3-13,0] | 34,3 [34,3-35,0] | 243 [217-273] | 1,8 [1,8-2,5] |
| 5.000 | sabit | 5.000 [4.641-5.000] | 5,1 [0,9-5,2] | 7,1 [2,9-7,2] | 5.098 [1-5.259] | 24,2 [21,0-29,8] |
| 5.000 | üstel | 5.000 [5.000-5.000] | 24,1 [24,1-24,2] | 25,1 [25,1-25,2] | 0 [0-0] | 12,2 [7,8-18,5] |
| 5.000 | rastgeleli üstel | 1.263 [1.219-1.285] | 12,1 [12,1-12,1] | 35,1 [32,1-35,1] | 576 [253-644] | 6,8 [3,7-22,6] |

## Hipotezlerin durumu

| # | Hipotez | Sonuç |
|---|---|---|
| H1 | Rastgeleli kural kesintideki tepe denemeyi düşürür | **Desteklendi.** Tepe deneme N'in ~%24-25'ine indi (2.000'de 484, 5.000'de 1.263); diğer iki kuralda tepe = N |
| H2 | Rastgeleli kural TTR'yi uzatır | **Desteklendi (TTR %100).** 34-35 sn, sabit kuralın ~5 katı, üstelin ~1,4 katı. Ama **TTR %50 daha kısa** (12 sn, üstel 24 sn): uzayan uç kısım |
| H3 | Sunucu bu ölçekte zorlanmaz | **Desteklendi.** Sunucu CPU'su en çok %30 (sınır 200). Üstel kuralda sunucu açıldığında 5.000 cihazın tamamı **ilk denemede** bağlandı (başarısız bağlantı 0), saniyede ~5.000 eşzamanlı bağlantı kurulumuna rağmen |
| H4 | N = 2.000 ile 5.000 arasında devrilme noktası yok | **Desteklendi.** Aynı kural için TTR %100 medyanları N'ye göre ≤ 1 sn farklı (sabit 6,3→7,1; üstel 25,2→25,1; rastgeleli 34,3→35,1). Yalnızca iki N, sunucu zorlanmadığı için sınırlı kanıt |

## Yorum

1. **Bilinen eğilim yeniden üretildi (H1, H2):** Rastgelelik tepeyi düşürür, toparlanmanın sonunu uzatır. Ortamın doğru çalıştığının sağlamasıdır; yeni bulgu değil.
2. **Bu ortamda tepeyi düşürmenin bir faydası ölçülemedi.** Sunucu zaten zorlanmıyor (H3). Dolayısıyla rastgeleli kuralın bedeli (TTR %100: 6-7 sn → 34-35 sn) ölçüldü, **faydası ölçülemedi.** "Rastgele bekleme gereksizdir" denmemeli: bu bulgu TLS'siz, tek sunuculu, sunucunun yetiştiği bir ortama özgü.
3. **Rastgelesiz kurallarda TTR yükten değil, yeniden deneme takviminden gelir.** Üstel kuralda üç tohum ve iki N için TTR %100 = 25,1 sn, bu elle hesaplanan değerle birebir uyuşuyor: cihazlar kopmadan sonra t = 1, 8, 17, 30, 51, 86 sn'de deniyor (bekleme 1, 2, 4, 8, 16, 30 + 5 sn zaman aşımı); sunucu t ≈ 60,9 sn'de açılıyor, 51'deki deneme başarısız oluyor (sunucu kapalı), sonraki deneme 86'da: 86 − 60,9 = 25,1 sn. **Sonuç, kesintinin süresine (takvimdeki fazına) bağlı bir testere dişi fonksiyon.** 45 sn'lik bir kesintide TTR ~1-6 sn olurdu. Bu yüzden "üstel kuralın TTR'si 25 sn" tek başına anlamsız; **kesinti süresi taraması** gerekiyor (önerilen duyarlılık deneyi).
4. Sabit kuralda `N=5.000, tohum 1` koşusu (TTR %100 2,9 sn, başarısız 1) diğer iki tohumdan (7,1-7,2 sn, ~5.100) farklı: faz etkisi (sunucunun açıldığı an, 6 sn'lik deneme döngüsünün neresine denk geldi). **Aykırı olarak çıkarılmadı.**
5. **Kararlılık:** IP ile ölçüm, adla ölçümdeki 27-69 sn'lik dalgalanmaya kıyasla çok kararlı (üstel ve rastgeleli hücrelerde aralıklar dar). Bu, Bulgu 5'in (DNS artefaktı) bir doğrulamasıdır.

## Sınırlar

- Hücre başına **3 koşu**: medyan ve aralık verildi, güven aralığı iddiası yok.
- **Kesinti süresi tek (60 sn).** TTR'nin faza bağımlılığı nedeniyle sonuçlar bu süreye özgüdür (Yorum 3).
- TLS'siz, kimlik doğrulamasız, tek sunucu, sıfır gecikmeli ağ, tek makine, sanal cihaz. Mutlak kapasite iddiası yok.
- Sunucu CPU örnekleme ~0,7 Hz; çok kısa tepeler kaçmış olabilir. Simülatör CPU tepesi en çok %245 (sınır 400, yani ~%61): yük üreticisi sınıra dayanmadı, ama yüksek.
- Koşular sırasında Docker'da başka bir projeye ait iki konteyner (`kaizera-postgres`, `kaizera-redis`) **boşta** çalışıyordu. Etkisi ölçülmedi; küçük bir gürültü kaynağı olabilir.
- Kesinti yalnızca temiz tür. Bağlantı sonrası yük (eşitleme, biriken mesaj boşaltma) ve alarm gecikmesi bu matriste **ölçülmedi**.
- Zaman çözünürlüğü 1 sn, TTR ±1 sn.

## Dosyalar

`results/adim3_*_v5.*` (18 koşu), `results/matris_*.txt` (sıra ve ilerleme; ilk satırdaki iptal notu dahil), `results/ozet_adim4*.{txt,csv}`, `scripts/matris.ps1`, `scripts/tekrar_ozet.py`, şekiller `makale/sekiller/sekil_matris_ozet.png`, `sekil_deneme_n5000_v5.png`