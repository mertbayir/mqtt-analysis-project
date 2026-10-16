# 11 — Adım 6b: TLS ile bağlantı sonrası yük

Tarih: 17 Ekim 2026
Durum: **Matris başlıyor** (hipotezler sonuçlar görülmeden önce yazıldı; TLS ile bağlanma aşaması sonuçları (günlük 10) ve TLS'siz bağlantı sonrası sonuçlar (günlük 06) yazım sırasında biliniyordu, TLS'te bağlantı sonrası iş hiç koşulmamıştı)
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

`[ ]` Matris bitince doldurulacak.
