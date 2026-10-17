# Katkı ve bilimsel yön (yalın özet)

Tarih: 17 Ekim 2026. Bu dosya, sonuçların ne ifade ettiğini ve çalışmanın bilimsel yönünü yalın dille toplar. Sayıların kaynağı: `project/docs/deney-gunlugu/` (günlükler 05, 06, 07, 08, 10, 11, 12, 13) ve `makale/taslak.md` (Bölüm 5).

## 1. Hangi problem?

Çok sayıda cihazın tek bir sunucuya bağlı olduğu sistemde sunucu bir süre kapanıp açılınca cihazlar aynı anda geri döner, sonra da kesintide biriktirdikleri işi yaparlar (durum eşitleme, biriken mesajları gönderme). Aynı sunucu o sırada **canlı mesajları** da taşıdığı için bu iş canlı mesajları (bizim senaryomuzda alarmları) geciktirebilir.

## 2. Çıktı değerleri ne anlama geliyor?

| Değer | Anlamı | Örnek |
|---|---|---|
| **Canlı alarm gecikmesi (p99)** | Alarm mesajlarının %99'unun bu süreden kısa sürede ulaştığı süre | Normalde ≈ 30 ms; kesinti sonrası 9.158 ms (Mosquitto, 5.000 cihaz, üstel kural, hemen boşaltma) |
| **Tepe** | Sunucu açıldıktan sonraki en kötü saniye | "En kötü an 9 sn" |
| **100 ms üstü süre** | Gecikmenin "bozuk" kaldığı saniye sayısı | 19 sn |
| **Boşaltma bitişi** | Biriken verinin tamamının ulaşma süresi | B1'de 43 sn, B2'de 66 sn |
| **Birikmiş alarmın yaşı** | Kesintide üretilen alarmın ulaştığı andaki yaşı (üretim anından beri geçen süre) | B2'de medyan 94 sn, B3'te 37 sn (10.000 cihaz) |
| **Sunucu CPU** | Sunucunun ne kadar dolduğu (100 = bir çekirdek) | %98: tek çekirdek tam dolu |

## 3. Neye çözüm bulduk, ne iyileşti?

Sorunun **kaynağını ayırdık** ve **hangi önlemin ne zaman işe yaradığını ölçtük**:

- Gecikmenin asıl kaynağı geri dönüşün kendisi değil, **döndükten sonraki iş**. (B0 kontrolünde dönüş tek başına gecikme yaratmadı: 47-90 ms.)
- **Boşaltmayı zamana yaymak (B2):** üstel kuralda alarm gecikmesi 9.158 → 143 ms (%98). 10.000 cihazda varsayılan ayar yetmedi; ayar büyütülünce yaklaşık 7 sn → 170-280 ms.
- **Dönüşü rastgele dağıtmak:** TLS'te toparlanma 63 sn → 29 sn; sunucu açıkken başarısız bağlantı sıfır.
- **Alarmı öne almak (B3, bizim küçük tasarımımız):** birikmiş alarmın yaşı B2'ye göre %24-60 azaldı, canlı alarm korundu (üç hücrede). Bir hücrede (üstel dönüş + TLS) canlı alarm B2'den 2,3 kat kötüleşti.
- **Bütün bunlar bizim test düzeneğimizde ölçüldü;** her ortamda geçerli bir garanti değildir.

## 4. Bilimsel yön ve katkı türü

Çalışma **yeni bir algoritma önermez**; deneysel bir ölçüm çalışmasıdır. Katkıları:

1. **Bilinen önlemin sınırını göstermek.** Rastgele bekleme (RFC 6455, RFC 5626) bağlanma yükünü azaltır ama bağlandıktan sonraki yükü tek başına çözmez; bunu ayrı ölçtük.
2. **"En iyi politika ortama göre değişir."** TLS yokken ve sunucu rahatken dönüşü dağıtmak yeter; TLS ya da çok cihaz varsa boşaltmayı da yaymak gerekir. Tek ve evrensel çözüm yok.
3. **Çekirdek eklemek tek başına yetmiyor** (EMQX 2 → 4 CPU: yalnızca %29 iyileşme).
4. **Yeni bir bedel ölçütü:** birikmiş alarmın yaşı. Boşaltmayı geciktirmek canlı alarmı korurken birikmiş alarmı geciktirir; bunu ölçtük ve B3 ile azalttık.
5. **Ölçüm güvenilirliği bulguları:** WSL2 duvar saati sıçraması, Docker DNS, zaman aşımı hatası, EMQX varsayılan koruma ayarının ölçümü bozması.

## 5. Dürüst sınırlar

- Tek bilgisayar, sanal cihaz, temiz kesinti, TLS sertifikası doğrulanmadı, iş yükü varsayımsal; klinik güvenlik iddiası yok.
- İlgili çalışmalar henüz okunmadı; "ilk kez" denemez, yalnızca "bildiğimiz kadarıyla bulunamadı".
- B3 küçük bir tasarım fikridir, genel bir çözüm değildir; bazı koşullarda bedeli vardır.
- Canlı simülasyon (web arayüzü) bir **modeldir, ölçüm değildir**; makaleye girmez.
- Küçük mesaj kayıpları (39+30 koşunun bir kısmında) açıklanamadı.

## 6. Ödevde tek cümle

> Sunucu kesintisi sonrası toplu geri dönüşte canlı mesaj gecikmesinin asıl kaynağını ayırt ettik; iki sunucuda, TLS'li ve TLS'siz koşullarda hangi önlemin ne zaman işe yaradığını ölçtük ve bedelini (birikmiş verinin gecikmesini) de gösterdik.
