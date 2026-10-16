# Faz 2 yol haritası: kapsamı genişletme

Tarih: 16 Ekim 2026
Karar: Faz 1 (adım 1-5c) bitti. Süre kısıtı yok, kapsam genişletiliyor. Amaç: makaleyi "tek bir ortamda bir gözlem" olmaktan çıkarıp daha genel ve daha güçlü hâle getirmek.

## Faz 1'de elde edilenler ve açık kalanlar

| Bulgu | Açık kalan |
|---|---|
| Bağlantı sonrası iş (eşitleme + boşaltma) canlı alarm gecikmesini bağlanmanın kendisinden çok daha fazla artırıyor; dönüşü dağıtmak ve boşaltmayı yaymak (B2) işe yarıyor, parametreler N'ye bağlı | Sunucu bağlanma aşamasında hiç zorlanmadı (TLS yok). Yalnızca Mosquitto (tek çekirdek). Yalnızca temiz kesinti. Biriken alarmlar vital ile aynı sırada gidiyor |
| Üstel kuralın toparlanma süresi bir takvim sonucu | Tek kesinti süresi (60 sn) denendi |

## Adaylar

| # | Deney | Neden değerli | Maliyet | Risk |
|---|---|---|---|---|
| F1 | **TLS** (Adım 6) | Gerçek sistemler TLS kullanır. TLS el sıkışması tek çekirdekli sunucuda pahalıdır; bağlanma aşamasında sunucuyu **gerçekten** zorlayabilir (Adım 4'te zorlanmadı). Makale 5.4'ü doldurur, rastgeleli kuralın bu ölçekte faydasını ilk kez ölçebiliriz | Orta: test sertifikası, sunucu ayarı, istemci kodu | El sıkışma maliyeti simülatörü de zorlayabilir |
| F2 | **Kesinti süresi taraması** | Üstel TTR'nin testere dişi olduğunu ölçerek göstermek (şu an yalnızca hesap) | Düşük: mevcut betik, kısa koşular | Düşük |
| F3 | **Alarm önceliği (B3)**: birikmiş alarmlar önce, vital sonra | Tıbbi çerçevenin anlamlı ölçütü: "birikmiş bir alarm ne kadar geç ulaşıyor". B1, B2'de alarm da vital sırasını bekliyor | Orta: cihaz boşaltma sırası + yeni ölçüt | Düşük |
| F4 | **Sessiz kesinti** (ağ sessizce kopar) | Gerçekte çoğu kesinti temiz kapanma değil; cihazlar kopmayı kalp atışı zaman aşımıyla geç ve dağınık fark eder | Orta: ağ kesme yöntemi | Orta: Docker ağında güvenilir kesme gerekir |
| F5 | **Ikinci sunucu (EMQX)**: çok çekirdekli | "İkinci izdiham tek çekirdekli Mosquitto'ya özgü mü?" sorusunun cevabı; makalenin genelleme gücü | Yüksek: yeni sunucu, ayar, kaynak eşitleme | Orta: ayar farkları karşılaştırmayı bulandırır |
| F6 | **N ölçekleme**: B2 parametresi N'ye göre (N = 2.500-20.000) | "B2 N ile ölçeklenmeli" iddiasını iki noktadan bir eğriye çıkarmak | Orta-yüksek: çok koşu, N = 20.000 simülatörü zorlar | Orta |
| F7 | **Kenar geçidi katmanı** (gateway) | Hasta başı hub: 10 cihaz tek bağlantı. Bağlantı sayısını 10 kat düşürür; soruyu "geçit var mı yok mu"ya genişletir | Yüksek: yeni bileşen | Yüksek: kapsamı büyütür |

## Önerilen sıra

**F1 (TLS) → F5 (EMQX) → F3 (alarm önceliği) → F2 ve F4 → F6 → F7.**

Gerekçe: F1 ve F5, çalışmanın iki asıl zayıflığını (sunucu zorlanmaması, tek sunucu) doğrudan giderir; F3 tıbbi çerçeveyi ölçülebilir kılar. F2-F4 mevcut düzeneği sağlamlaştırır. F6-F7 en çok emek isteyen, en çok genişleten işler.

## Çalışma kuralı

Her adım için: önce günlükte tasarım ve hipotezler (sonuçlardan önce), sonra koşu, sonra sonuç; hatalı koşular silinmez, işaretlenir; makale taslağı her adımdan sonra güncellenir.
