# 15 — Adım 9: Sessiz kesinti

Tarih: 17 Ekim 2026
Durum: **Matris başlıyor** (hipotezler sonuçlardan önce yazıldı; çalışma testlerinin sonuçları yazım sırasında biliniyordu, bkz. "Çalışma testleri ve bulunan sorunlar")
Rapor bölümü: Bulgular (5.11), Sınırlar (temiz kesinti sınırının kaldırılması)

## Amaç

Şimdiye kadarki tüm deneylerde kesinti **temizdi**: sunucu `docker stop` ile kapanıyor, bağlantılar anında sonlanıyor, cihazlar kopmayı hemen öğreniyordu. Gerçek kesintilerin çoğu **sessizdir** (ağ ya da yol kopar, sunucu "yok" olur): cihazlar kopmayı ancak kalp atışı zaman aşımıyla (kalp atışı 10 sn) ve **farklı anlarda** fark eder. Soru: kopmanın bu dağınık fark edilmesi, toplu yeniden bağlanma izdihamını ve bağlantı sonrası yükü azaltır mı?

## Kurulum

Adım 5 (günlük 06, Mosquitto, TLS'siz, N = 5.000) ile aynı; fark yalnızca kesintinin türü:

| Tür | Yöntem |
|---|---|
| **temiz** | `docker stop mqtt-broker`, 60 sn sonra `docker start` (önceki gibi) |
| **sessiz** | `docker network disconnect project_default mqtt-broker` (sunucu çalışmaya devam eder ama ağdan kopar: hiçbir paket gidip gelmez, soket kapanmaz), 60 sn sonra `docker network connect --ip <aynı IP> --alias broker project_default mqtt-broker` |

Cihazlar kopmayı kalp atışı zaman aşımıyla (≈ 10-15 sn) fark eder. Zaman hizalaması: sunucunun açıldığı an artık **olay zamanlarından** (`olaylar.txt`) alınır (temiz kesintide `kopma + kapalı süre` yöntemi bağlı sayısının sıfıra düştüğü ana dayanıyordu; sessizde düşüş 10-15 sn'ye yayıldığı için kullanılamaz). Hizalama duyarlılığı günlük 12'de gösterildi (±2,4 sn sonucu değiştirmedi).

## Çalışma testleri ve bulunan sorunlar (N = 500, tek koşu; nihai veri değil)

1. **Yeniden bağlanma:** `docker network connect --ip` Docker'ın otomatik oluşturduğu ağda desteklenmedi ("user specified IP address is supported only when connecting to networks with user configured subnets"). Çözüm: `docker-compose.yml`'e sabit alt ağ (172.21.0.0/16, Docker'ın önceden otomatik seçtiği ile aynı) ve sunucuya sabit IP (172.21.0.2, zaten bu adresi alıyordu) eklendi; deney koşulu değişmedi.
2. **Cihazlar sessiz kesintiyi hiç fark etmedi.** Ağdan koparılan sunucuyla 40 sn boyunca bağlı sayısı 500'de kaldı, ağ geri gelince bir kısmı düştü. Neden: cihazlar sürekli yayın yaptığından kalp atışı kontrolü tetiklenmiyor; yayın onayı (PUBACK) beklerken MQTTnet kendi zaman aşımını (5 sn) hata olarak veriyor ama bağlantıyı kapatmıyor. Bu, onay zaman aşımı olmayan bir istemcinin sessiz kesintide süresiz askıda kalabileceği anlamına gelir (gerçek bir bulgu).
3. **Düzeltme (deney koşulu):** `-AckSn` bayrağı (`ACK_TIMEOUT_SEC`): yayın onayı zaman aşımına uğrarsa cihaz bağlantıyı kapatır ve yeniden bağlanma kuralı devreye girer. Varsayılan 0 (önceki adımlardaki davranış, değişmedi). **Bu matriste her iki kesinti türünde de `AckSn = 10` açıktır** (karşılaştırma aynı istemciyle yapılsın diye); Adım 5-8 sonuçlarıyla (AckSn kapalı) doğrudan karşılaştırılmaz.
4. **Çalışma testi sonucu (düzeltmeden sonra):** cihazlar kesintiyi 6-13 sn içinde fark etti; bağlı sayısı 500'den 0'a ≈ 4 sn'de indi. Bu, HS1'in (≥ 8 sn yayılma) muhtemelen çürüyeceğini gösteriyor (yayınlar 2 sn'de bir yapıldığı için zaman aşımları birbirine yakın). HS1 yine de matriste kayıtlı kalır.

## Hipotezler (matris öncesi yazıldı)

| # | Hipotez | Not |
|---|---|---|
| HS1 | **Sessiz kesintide** bağlı cihaz sayısının sıfıra inmesi ≥ 8 sn'ye yayılır (temiz kesintide < 2 sn) | Kalp atışı zaman aşımının dağınıklığı; **çürüyebilir** |
| HS2 | **Üstel + B1, sessiz kesinti:** canlı alarm p99 tepesi, aynı blokta temiz kesintideki değerin en çok %50'sidir (kopma anları dağıldığı için geri çekilme takvimleri kayar, izdiham yayılır) | **Çürüyebilir** |
| HS3 | **Rastgeleli + B1:** iki kesinti türünde de tepe < 300 ms (fark yok denecek kadar azdır) | **Çürüyebilir** |
| HS4 | Sessiz kesintide cihaz tarafı gönderim hatası sayısı (zaman aşımına uğrayan yayınlar) temiz kesintiden büyüktür (cihazlar kopmayı fark edene kadar yayın yapıp onay beklemeye devam eder) | **Çürüyebilir** |
| HS5 | Tüm koşularda tekrar = 0; kayıp ≤ 1 | **Çürüyebilir** (sessiz kesintide kayıp daha olası: cihazlar kopmadan önce gönderdiği ama sunucunun alamadığı mesajlar) |

## Tasarım

| Öğe | Değer |
|---|---|
| Faktörler | kesinti türü (temiz, sessiz) × A (üstel, rastgeleli üstel) × B1 = 4 hücre |
| N, sunucu | 5.000, Mosquitto, TLS'siz |
| Tekrar | 3 blok (tohum 1-3), blokta rastgele sıra, 12 koşu |
| Zaman çizelgesi | yayılım 20 sn, kesinti t = 40 sn'de 60 sn, izleme 100 sn (sessizde cihazlar kopmayı 6-13 sn geç fark eder) |
| Sürüm | `s9` (koşu adı `adim5_<A>_b1[-sessiz]_n5000_s<tohum>_s9`; sessiz hücre B etiketinde `-sessiz` taşır) |
| Komut | `./scripts/matris5.ps1 -Tekrar 3 -Cihazlar 5000 -BPolitikalari b1 -KesintiTurleri temiz,sessiz -AckSn 10 -SonrasiSn 100 -Surum s9` |
| Süre | ≈ 12 × 4,5 dk ≈ 55 dk |

## Sonuçlar

`[ ]` Matris bitince doldurulacak.

## Sınırlar (önceden bilinen)

- Sessiz kesinti, ağdan kopmanın Docker ile taklididir (tek yönlü paket kaybı, kısmi kopma, gecikme dalgalanması yok).
- Bu bütçede yalnızca B1 ve iki bekleme kuralı denendi; B2/B3, TLS ve EMQX sessiz kesintide denenmedi.
- Toplayıcı da aynı ağdan koptuğu için (o da bir istemci) ölçüm bağlantısı kesintiyi yaşar; toplayıcının yeniden bağlanması sunucu açıldıktan sonra yaklaşık 0,5 sn sürer (günlük 09).
- Adım 5'in tüm sınırları geçerli.
