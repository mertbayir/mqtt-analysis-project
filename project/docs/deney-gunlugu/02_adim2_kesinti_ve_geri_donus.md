# 02 — Adım 2: 100 cihaz, sunucu kesintisi, yeniden bağlanma

Tarih: 11 Ekim 2026
Durum: **Tamamlandı** (ilk koşuda 2 hata bulundu ve düzeltildi, ikinci koşu geçerli)
Rapor bölümü: Yöntem / Simülatör tasarımı; Bulgular (ön gözlem)

## Amaç

Simülatörün temel döngüsünü kurmak (bağlan, kopmayı fark et, bekle, tekrar bağlan) ve temiz kesintide cihazların nasıl davrandığını ilk kez gözlemek.

## Kurulum

| Öğe | Değer |
|---|---|
| Cihaz sayısı | 100 |
| Yeniden bağlanma politikası | `SabitBekleme`, 1 sn (tohum 1) |
| Kalp atışı | 10 sn |
| Bağlantı zaman aşımı | 5 sn |
| Kesinti türü | Temiz: `docker stop mqtt-broker` |
| Zaman çizelgesi | t=15 sn'de durdur, 20 sn kapalı, sonra `docker start` |
| Toplam süre | 75 sn |
| Simülatör | Windows'ta çalışıyor, `localhost:1883` üzerinden (Docker port yönlendirmesi) |
| Betik | `project/scripts/adim2.ps1` |
| Ham sonuçlar | `project/results/adim2.csv`, `adim2_konsol.txt`, `adim2_olaylar.txt` |

## Gözlemler (ikinci koşu)

| Zaman | Olay |
|---|---|
| t=15 sn | Sunucu durdu. **Bağlı sayısı aynı saniyede 100 → 0.** Temiz kesintide cihazlar kopmayı anında öğrendi |
| t=16–32 sn | Cihazlar her ~5-6 sn'de bir deneme yaptı. **Denemeler hep "dalga" hâlinde geldi:** bir saniyede 100 deneme, sonraki 4-5 saniyede 0 |
| t≈36 sn | Sunucu geri açıldı. t=36'da 94, t=37'de **100 cihaz bağlı** |
| Hata türleri | `MqttCommunicationException` 300, `MqttConnectingFailedException` 100 |

**Toparlanma süresi (bu koşuda):** sunucu açıldıktan ~1-2 sn sonra 100/100 cihaz bağlı. 100 cihaz için sorun yok.

### Dalgalanma (senkronizasyon) gözlemi

Sabit 1 sn bekleme kuralıyla 100 cihaz **birlikte hareket etti:** hepsi aynı anda dener, hepsi aynı anda başarısız olur, hepsi aynı anda 1 sn bekler. Deneme sayısı grafiğinde tek saniyelik tepeler oluştu (`deneme/sn` = 100, 94 + 6). Bu, projenin konusu olan "toplu yeniden bağlanma"nın küçük ölçekli bir örneği. Cihaz sayısı büyüdükçe tepe büyüyecek.

Rastgele bekleme (jitter) bu tepeyi yaymak için kullanılacak (politika A2).

## Bulunan ve düzeltilen hatalar (simülatör)

1. **Bağlı sayısı eksiye düşüyordu** (-300). MQTTnet `DisconnectedAsync` olayını **hiç bağlanamayan denemelerde de** tetikliyor. Sayaç yalnızca cihaz gerçekten bağlıyken düşürülecek şekilde düzeltildi (`_bagli` bayrağı).
2. **Konsol çıktısında Türkçe karakter bozuluyordu.** `Console.OutputEncoding = UTF8` eklendi. (Dosya doğruydu, bozuk görünen yalnızca PowerShell 5.1'in dosyayı ANSI sanarak göstermesiydi. `Get-Content -Encoding UTF8` ile okunmalı.)

**Ders:** Ölçüm kodundaki sayaçlar önce küçük ölçekte doğrulanmalı. Yanlış sayaçla alınan sonuç raporlanamaz.

## Notlar ve açık sorular

- Hata sınıflaması başladı (`ex.GetType().Name`). İki ayrı hata türü görüldü ama **ayrıntıya girilmedi**: `MqttCommunicationException` (konuşulamadı) ile `MqttConnectingFailedException` (sunucu bağlantıyı reddetti ya da cevap vermedi) neden farklı zamanlarda çıktı, henüz bilmiyoruz. Adım 4'te ölçüm kurarken ayrıştırılacak.
- Simülatör Windows'ta, sunucu Docker'da çalıştığı için trafik Docker Desktop'ın port yönlendirmesinden geçiyor. Planda "simülatör de Docker ağı içinde" olacak (15a §1). Büyük N'de bunu değiştireceğiz.
- Bu koşu tek tekrar. Hiçbir istatistik iddiası yok, yalnızca ilk gözlem.

## Ek: A1 politikası (üstel geri çekilme) eklendi

`UstelGeriCekilme(ilkSn: 1, tavanSn: 30)`: bekleme = `ilk × 2^(denemeNo−1)`, en çok 30 sn. Rastgelelik yok.

Cihazlar ve sunucu olmadan kuralın kendisi test edildi (`--politika-testi`):

| Deneme | 1 | 2 | 3 | 4 | 5 | 6+ |
|---|---|---|---|---|---|---|
| Bekleme (sn) | 1 | 2 | 4 | 8 | 16 | 30 |

Politika artık `POLICY` ortam değişkeniyle seçilir (`sabit` veya `ustel`). Bu sayede aynı program farklı kurallarla çalıştırılabilir. A2 (rastgele ilk bekleme + üstel) henüz yazılmadı.

## Sonraki adım

A2 politikasını yazmak, sonra aynı deneyi cihaz sayısını artırarak tekrarlamak (Adım 3).

## Dosyalar

- `project/src/Simulator/Program.cs`, `Device.cs`, `ReconnectPolicy.cs`, `Stats.cs`
- `project/scripts/adim2.ps1`
- `project/results/adim2*.{csv,txt}`
