# 00 — Deney Ortamı

Raporun "Yöntem / Deney ortamı" bölümü için ham kayıt. Ortam değiştikçe **yeni satır ekle**, eskisini silme (tarihle).

Kayıt tarihi: 11 Ekim 2026

## Donanım ve işletim sistemi

| Öğe | Değer |
|---|---|
| İşlemci | Intel Core i7-11600H @ 2.90 GHz, 6 çekirdek / 12 mantıksal |
| RAM | 15,7 GB |
| İşletim sistemi | Windows 11 Home Single Language 10.0.26200 |
| Güç planı | High performance (Yüksek performans) |
| Ekran kartı | RTX 3060 (projede kullanılmıyor) |

## Docker / WSL2

| Öğe | Değer |
|---|---|
| Docker Desktop | Docker Engine 28.4.0 (istemci ve sunucu) |
| Docker'ın gördüğü kaynak | 12 CPU, ~8,2 GB bellek (WSL2 varsayılanı) |
| `.wslconfig` | **Yok, varsayılan ayarlar.** Büyük N denemelerinden önce bellek/CPU sınırı açıkça yazılacak |

## Yazılım sürümleri

| Öğe | Sürüm |
|---|---|
| MQTT sunucusu | Eclipse Mosquitto **2.1.2** (`eclipse-mosquitto:2`) |
| Mosquitto imaj özeti | `sha256:38c0da4f2ef84284d47b3b3eeea1cb3bdeabe81ee10caf0cd5c5ff61ee3ea408` |
| .NET SDK (kurulu) | 8.0.413, 9.0.308, 10.0.101 |
| Proje hedefi | `net8.0` |
| MQTT istemci kütüphanesi | MQTTnet **5.2.0.1603** (NuGet) |
| MQTT protokol sürümü | 5.0 |
| Git | 2.50.0.windows.2 |

## Ölçüm sırasında uyulacak koşullar (henüz uygulanmadı)

- Priz takılı, güç planı "Yüksek performans".
- Antivirüs taraması, güncellemeler ve tarayıcı ölçüm sırasında kapalı veya her çalıştırmada aynı durumda.
- Her çalıştırmanın başında benzer makine sıcaklığı.
- Sunucu ve sanal cihazlar için CPU/bellek bölüşümü tüm çalıştırmalarda **aynı**.
- Sonuçlar **bu makineye göre göreceli** raporlanır, mutlak kapasite iddiası yok.

## Değişiklik geçmişi

| Tarih | Değişiklik |
|---|---|
| 11 Ekim 2026 | İlk kayıt |
