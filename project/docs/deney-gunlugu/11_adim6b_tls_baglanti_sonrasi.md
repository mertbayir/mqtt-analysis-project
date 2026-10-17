# 11 — Adım 6b: TLS ile bağlantı sonrası yük

Tarih: 17 Ekim 2026
Durum: **Matris tamamlandı (18/18 koşu, sürüm tls6)**. Hipotezler sonuçlar görülmeden önce yazıldı; TLS ile bağlanma aşaması sonuçları (günlük 10) ve TLS'siz bağlantı sonrası sonuçlar (günlük 06) yazım sırasında biliniyordu, TLS'te bağlantı sonrası iş hiç koşulmamıştı)
Rapor bölümü: Bulgular (5.8 TLS'te bağlantı sonrası yük), Tartışma
Önceki adımlar: `06_adim5_baglanti_sonrasi_yuk.md` (TLS'siz), `10_adim6_tls.md` (TLS, yalnızca bağlanma)

## Amaç

Gerçek bir dağıtımda iki yük birlikte gelir: yeniden bağlanma el sıkışmaları (TLS) ve bağlantı sonrası iş (eşitleme + boşaltma). Adım 5 yalnızca ikincisini (TLS'siz), Adım 6 yalnızca birincisini ölçtü. Bu adım ikisini birlikte ölçer. Sorular: (i) TLS'te bağlanma aşamasının kendisi (B0) canlı alarm gecikmesine yansıyor mu? (ii) Bağlantı sonrası politikaların (B1, B2) sıralaması TLS'te değişiyor mu, özellikle N = 5.000'de rastgeleli A + B1 (TLS'siz 51 ms)? 

## Kurulum

Adım 5 (günlük 06, sürüm v6) ile aynı tasarım ve betikler (`matris5.ps1 -Tls`), tek fark cihazların sunucuya **TLS ile (8883)** bağlanması (RSA-2048, test sertifikası, doğrulama yok; Adım 6 ile aynı). **Toplayıcı TLS'siz** (1883) bağlanır; böylece ölçüm bağlantısı el sıkışma yükünden etkilenmez, ama sunucunun tek iş parçacığını aynı şekilde paylaşır.

| Öğe | Değer |
|---|---|
| Faktörler | A (üstel, rastgeleli üstel) × B (b0, b1, b2 = 30 sn / 200 ms) = 6 hücre |
| N | 5.000 |
| Tekrar | 3 blok (tohum 1-3), blokta rastgele sıra; 18 koşu |
| Zaman çizelgesi | Adım 5 ile aynı (yayılım 20 sn, kesinti t = 40 sn'de 60 sn); **izleme 180 sn** (v6'da 90 sn; TLS'te toparlanma 60-95 sn sürebildiği için) |
| Sürüm | `tls6` (koşu adı `adim5_<A>_<B>_n5000_s<tohum>_tls6`) |
| Komut | `./scripts/matris5.ps1 -Tekrar 3 -Cihazlar 5000 -Tls -SonrasiSn 180 -Surum tls6 -Not "Adim 6b TLS baglanti sonrasi"` |
| Analiz | `python scripts/analiz_adim5.py tls6` (aynı ölçütler) |
| Karşılaştırma | v6 (TLS'siz, aynı hücreler); aynı blokta değil (**sınır**) |
| Süre | ≈ 18 × 5,5 dk ≈ 100 dk |

## Hipotezler (matris öncesi yazıldı)

| # | Hipotez | Not |
|---|---|---|
| HF1 | **TLS'te B0** ile bile üstel A'da canlı alarm p99 tepesi tabanın en az 5 katıdır (TLS el sıkışma yükü canlı trafiğe yansır); rastgeleli A'da 5 katın altında kalır | **Çürüyebilir**; TLS'siz B0'da tepe 1,5 katın altındaydı |
| HF2 | **Üstel A + B1, TLS'te** canlı alarm p99 tepesi TLS'siz değerden (9.158 ms) yüksektir (el sıkışmalar ve boşaltma aynı çekirdeği paylaşır) | **Çürüyebilir**: TLS dönüşü iki dalgaya böldüğü için daha düşük de çıkabilir |
| HF3 | **Rastgeleli A + B1, TLS'te** tepe tabanın en az 10 katıdır (TLS'siz 51 ms, ≈ 1,7 kat; el sıkışma yükü eklenince doyma) | **Çürüyebilir** |
| HF4 | **TLS'te B2** (30 sn / 200 ms), B1'e göre (aynı A) tepeyi en az %50 düşürür | **Çürüyebilir**: TLS'siz N = 10.000'de B2 bu ayarla işe yaramamıştı |
| HF5 | Tüm koşularda tekrar = 0 ve kayıp ≤ 1 mesaj | **Çürüyebilir**; kayıp geçmişte (günlük 09) açıklanamamıştı |

## Sınırlar (önceden bilinen)

- Karşılaştırma hücreleri (v6) aynı blokta değil; izleme süresi farklı (90 sn / 180 sn). Tepe ölçütü sunucu açıldıktan sonraki tüm saniyeler üzerindendir; v6'nın ilk 90 sn'si ile karşılaştırma geçerli, sonrası farklı.
- İstemci tarafı el sıkışma maliyeti ayrılamıyor; simülatör CPU'su zorlanabilir (Adım 6: en çok 320 / 400).
- Toplayıcı TLS'siz; gerçek bir sistemde toplayıcı da TLS'li olurdu.
- Adım 5 ve 6'nın tüm sınırları geçerli.

## Sonuçlar

### Koşu akışı (kayıt)

İlk matris (`results/matris5_20261017_0031.txt`) oturum kapandığı için 14. koşuda yarım kaldı: 13 koşu tamamdı, 14. koşu (rastgeleli / B1 / tohum 3) yarımdı; yarım koşunun dosyaları `results/iptal_tls6_yarim/` altına alındı (kayıt defterinde satırı yok, analize girmez). Kalan 5 koşu `-TamamlananiAtla` ile devam ettirildi (`results/matris5_20261017_1418.txt`; ilk 12 koşu "atlandı", blok 3'ün sırası yeniden rastgele seçildi). Toplam 18 koşu, 0 betik hatası. Çalışma arasında kod değişmedi.

### Hücre başına sonuçlar (N = 5.000, TLS; medyan [en küçük-en büyük]; parantez: TLS'siz v6 medyanı)

Kesinti öncesi taban (canlı alarm p99): TLS'te 27-49 ms (TLS'siz 26-37 ms).

| A | B | Alarm p99 tepe (ms) | >100 ms süre (sn) | Boşaltma bitişi (sn) | Sunucu CPU tepe (%) |
|---|---|---|---|---|---|
| üstel | B0 | **613** [429-689] (47) | 8 [7-8] (0) | - | 85,4 [58,6-96,0] (37) |
| üstel | B1 | 8.234 [6.637-8.915] (9.158) | 30 [30-31] (19) | 69,2 [68,9-70,8] (42,6) | 100,3 [99,5-101,0] (98) |
| üstel | B2 | 907 [661-1.021] (143) | 8 [6-14] (17) | 107,2 [106,8-108,6] (65,6) | 85,9 [78,2-86,4] (58) |
| rastgeleli | B0 | 63 [50-67] (60) | 0 [0-0] (0) | - | 45,8 [44,7-57,2] (21) |
| rastgeleli | B1 | **1.234** [977-3.578] (51) | 12 [9-21] (0) | 29,5 [29,4-30,8] (28,9) | 92,2 [89,3-92,8] (85) |
| rastgeleli | B2 | **137** [131-307] (79) | 2 [1-12] (0) | 68,7 [66,5-68,8] (63,7) | 75,4 [72,3-78,7] (59) |

Kayıp ve tekrar: 18 koşunun hepsinde 0 (üretilen ≈ 809 bin mesaj/koşu). Eşitleme zaman aşımı (sync_hata) 0.

### Hipotezlerin durumu

| # | Sonuç | Kanıt |
|---|---|---|
| HF1 | **Destekleniyor** | TLS'te B0: üstel A'da tepe 429-689 ms, kendi koşusunun tabanının 14-22 katı (TLS'siz 47 ms); rastgeleli A'da 50-67 ms, tabanın ≈ 2 katı (< 5) |
| HF2 | **Çürüdü** | Üstel + B1: TLS'te 8.234 ms, TLS'siz 9.158 ms; TLS'te daha yüksek değil, %10 düşük (aralıklar neredeyse örtüşüyor: 6.637-8.915 ve 8.821-10.413) |
| HF3 | **Destekleniyor** | Rastgeleli + B1, TLS'te tepe 977-3.578 ms, tabanın 30-115 katı (TLS'siz 51 ms) |
| HF4 | **Destekleniyor** | B2, B1'e göre tepeyi iki A'da da %89 düşürdü (üstel 8.234 → 907; rastgeleli 1.234 → 137) |
| HF5 | **Destekleniyor** | 18 koşunun hepsinde tekrar 0 ve kayıp 0 |

### Gözlemler

1. **TLS'te yeniden bağlanma yükü doğrudan canlı alarma yansıyor.** Bağlantı sonrası iş olmadan bile (B0) üstel kuralda alarm p99 tepesi 47 ms'den 613 ms'ye çıkıyor ve p99 yaklaşık 8 sn boyunca 100 ms'nin üstünde kalıyor; sunucu CPU tepesi %37'den %85'e çıkıyor. Rastgeleli kuralda bu etki yok (63 ms).
2. **Üstel + B1'de TLS ek bir kötüleşme getirmiyor** (8,2 sn'ye karşı 9,2 sn): bağlantı sonrası iş sunucuyu zaten doyuruyor (CPU %100). Boşaltma bitişi 43 sn'den 69 sn'ye uzuyor (TLS toparlanmayı iki dalgaya böldüğü için olabilir; test edilmedi).
3. **TLS'siz ortamda sorunsuz olan rastgeleli + B1 hücresi TLS'te doyuyor** (51 ms → 1.234 ms; tek koşuda 3,6 sn, sunucu CPU %92). TLS el sıkışma yükü ile boşaltma yükü aynı çekirdeği paylaşıyor.
4. **B2 (30 sn / 200 ms) TLS'te işe yarıyor:** rastgeleli + B2 tepe 137 ms ve CPU %75; bedel, boşaltmanın 29,5 sn'den 68,7 sn'ye uzaması. TLS'siz N = 10.000'de aynı B2 ayarı yetmemişti (günlük 07); burada yetiyor. Bu, günlük 08'deki "B2'nin yeterliliği toplam yüke bağlı" okumasıyla tutarlı, ama iki yük (TLS + N = 5.000 ile TLS'siz N = 10.000) aynı ölçekte karşılaştırılamaz.
5. **En iyi politika ortama göre değişiyor:** TLS'siz N = 5.000'de rastgeleli + B1 (51 ms, en hızlı boşaltma); TLS'te rastgeleli + B2 (137 ms). Politika seçimi, sunucunun ne kadar zorlandığına bağlı.
6. Üstel + B2'nin tepesi TLS'te 907 ms (TLS'siz 143 ms): üstel dönüşün TLS yükü B2 ile tam kapanmıyor.

### Sınırlar ve açık noktalar

- TLS'siz karşılaştırma (v6) aynı blokta değil; izleme 90 sn / 180 sn. Tepe ve eşik-üstü ölçütleri sunucu açıldıktan sonraki tüm saniyeler üzerindendir; v6'nın değerleri ilk 90 sn'de zaten sönümlenmişti.
- Toplayıcı TLS'siz; gerçek sistemde TLS'li olurdu.
- İstemci tarafı el sıkışma maliyeti ayrılamıyor; simülatör CPU tepesi ölçülmedi/raporlanmadı bu matriste (`sim.csv` var).
- Ek yük, ikinci dalga ve boşaltma gecikmesi için mekanizma doğrudan gözlenmedi.
- Rastgeleli/B1'de bir koşu (3,6 sn) diğer ikisinden (≈ 1,0-1,2 sn) belirgin ayrılır; aykırı olarak çıkarılmadı.
- Sertifika doğrulanmadı; TLS sürümü ve şifre takımı ölçülmedi.
