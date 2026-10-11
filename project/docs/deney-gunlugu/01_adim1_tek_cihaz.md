# 01 — Adım 1: Tek cihaz MQTT 5 bağlantısı

Tarih: 11 Ekim 2026
Durum: **Tamamlandı**
Rapor bölümü: Yöntem / Sistem kurulumu

## Amaç

Ortamın temel parçalarının çalıştığını doğrulamak: Docker'da MQTT sunucusu, .NET'te MQTT 5 istemcisi.

## Ne yapıldı?

1. `docker-compose.yml` ile Mosquitto 2.1.2 ayağa kaldırıldı (port 1883).
2. `mosquitto.conf`: `listener 1883` + `allow_anonymous true`. Mosquitto 2.x bu ikisi olmadan yalnızca yerel bağlantı kabul eder. **TLS ve kimlik doğrulama bu adımda yok.**
3. `src/Simulator` konsol projesi açıldı (.NET 8, MQTTnet 5.2).
4. Tek cihaz (`cihaz-0001`): MQTT 5, kalp atışı 10 sn, `CleanStart=true` ile bağlandı, `cihaz/0001/vital` konusuna abone oldu, QoS 1 ile `nabiz=72` yayınladı, mesajı geri aldı, bağlantıyı kapattı.

## Sonuç

```
Bağlanılıyor: localhost:1883
Bağlandı. Sonuç kodu: Success
[ALINDI] konu=cihaz/0001/vital mesaj=nabiz=72
Bağlantı kapatıldı.
```

İlk denemede çalıştı, hata yok.

## Çıkarımlar ve notlar

- **CONNACK** cevabı bağlanma sonucunu taşıyor. Fırtına sırasında reddedilen cihazların hangi nedenle reddedildiği (örn. 0x89 *Server busy*, 0x9F *Connection rate exceeded*) buradan okunacak. Hata sınıflaması bu noktaya bağlanacak.
- Şimdiki kurulum **TLS'siz.** Plan, bağlantı maliyeti düşük kalırsa fırtına hiç çıkmayabileceğini söylüyor (`14`, Bölüm 7, Risk 1). Adım 3'te (1.000 → 5.000 cihaz) TLS'siz ve TLS'li karşılaştırılacak.
- Windows'tan `localhost:1883` ile bağlandı. Gerçek deneyde simülatör Docker ağının içinde çalışacak (yayımlanmış port sıcak yolda kullanılmayacak, bkz. `15a` §1).

## Sonraki adım

Adım 2: 100 cihaz, `docker stop` / `docker start` ile sunucu kesintisi, cihazların yeniden bağlanmasını gözleme. İlk `ReconnectPolicy` burada yazılacak.

## Dosyalar

- `project/docker-compose.yml`
- `project/mosquitto/mosquitto.conf`
- `project/src/Simulator/Program.cs`
