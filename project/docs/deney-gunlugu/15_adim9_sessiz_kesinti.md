# 15 — Adım 9: Sessiz kesinti

Tarih: 17 Ekim 2026
Durum: **Matris tamamlandı (12/12 koşu, sürüm s9)**. (hipotezler sonuçlardan önce yazıldı; çalışma testlerinin sonuçları yazım sırasında biliniyordu, bkz. "Çalışma testleri ve bulunan sorunlar")
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

### Koşu akışı (kayıt)

Matris `results/matris5_20261017_2255.txt` (adı zaman damgalıdır), 12 koşu, 0 betik hatası; kod temiz (`4593548`). Analiz: `scripts/analiz_adim5.py s9` (sessiz kesintide başlama anı olay zamanlarından alınır). Bu matriste iki kesinti türünde de `AckSn = 10` açıktır.

**Doğrulama (sağlama):** temiz kesinti kolu, Adım 5'in (AckSn kapalı) aynı hücreleriyle uyumlu: üstel + B1 tepe 9.176 ms (Adım 5: 9.158), rastgeleli + B1 66 ms (Adım 5: 51); yani yayın onayı zaman aşımının açık olması temiz kesintide davranışı değiştirmedi (cihaz tarafı gönderim hatası 322-616).

### Hücre başına sonuçlar (N = 5.000, Mosquitto, TLS'siz, B1; medyan [en küçük-en büyük])

Taban 28-34 ms.

| Kesinti | A | Alarm p99 tepe (ms) | >100 ms süre (sn) | Boşaltma bitişi (sn)* | Cihaz tarafı gönderim hatası | Kopmayı fark etme (kesintiden itibaren) |
|---|---|---|---|---|---|---|
| temiz | üstel | **9.176** [9.167-9.256] | 19 [19-20] | 41,9 [41,5-42,4] | 433 [322-569] | ≈ 1 sn |
| sessiz | üstel | **5.858** [5.843-6.258] | 15 [14-16] | 14,6 [14,1-14,6] | **35.020** [34.705-35.024] | ≈ 11 sn |
| temiz | rastgeleli | 66 [54-69] | 0 | 28,8 [28,1-29,2] | 423 [325-616] | ≈ 1 sn |
| sessiz | rastgeleli | **638** [573-769] | 4 [3-4] | 33,1 [32,4-33,5] | **34.958** [34.869-35.115] | ≈ 11 sn |

\* Sunucunun açıldığı andan son biriken mesaja kadar. Sessizde cihazlar kesintiyi geç fark ettiğinden yeniden bağlanma takvimi kayar; bu yüzden süreler temiz ile birebir kıyaslanamaz.

Kayıp: 12 koşunun 11'inde 0, birinde 1 (rastgeleli, sessiz, tohum 2); tekrar 12 koşuda 0.

### Hipotezlerin durumu

| # | Sonuç | Kanıt |
|---|---|---|
| HS1 | **Çürüdü** | Sessizde bağlı sayısı kesintiden ≈ 11 sn sonra düşmeye başlıyor ama %95'ten %5'e **1-2 sn'de** iniyor (temiz: anında). Cihazlar kopmayı geç fakat **eşzamanlı** fark ediyor (hepsinin yayın onayı aynı anda zaman aşımına uğruyor); beklenen "dağınık fark etme" gözlenmedi |
| HS2 | **Çürüdü** | Üstel + B1, sessiz kesinti tepe 5.858 ms, temiz 9.176 ms: %36 düşük, %50 eşiği (≤ 4.588 ms) sağlanmadı |
| HS3 | **Çürüdü (sessizde)** | Rastgeleli + B1: temiz 66 ms (< 300), **sessiz 638 ms** [573-769] (≥ 300): sessiz kesinti rastgeleli kuralda tepeyi ≈ 10 kat artırdı |
| HS4 | **Destekleniyor** | Cihaz tarafı gönderim hatası sessizde ≈ 35.000 / koşu, temiz kesintide 322-616 (≈ 70-100 kat) |
| HS5 | **Destekleniyor** | Tekrar 12 koşuda 0; kayıp en çok 1 |

### Gözlemler

1. **Onay zaman aşımı olmayan bir istemci sessiz kesintide süresiz askıda kalıyor** (çalışma testi; yukarıdaki "Çalışma testleri" bölümü). Sürekli yayın yapan cihazda kalp atışı kontrolü de tetiklenmiyor: kopmayı yalnızca yayın onayı zaman aşımı ya da TCP yeniden iletim zaman aşımı (dakikalar) ortaya çıkarır.
2. **Sessiz kesinti, beklenen "dağınık fark etme" etkisini yaratmadı:** tüm cihazlar kopmayı ≈ 11 sn gecikmeyle ama birlikte fark etti. İzdiham dağılmadı.
3. **Üstel + B1'de tepe %36 düştü (9,2 → 5,9 sn), ama bu bir dağılma değil, takvim kayması gibi görünüyor:** cihazlar kesintiyi 11 sn geç fark edince geri çekilme takvimleri aynı kadar kayıyor, sunucu açıldıktan kısa süre sonra bir deneme dalgası denk geliyor ve birikmiş mesaj daha az (kesinti cihaz tarafında ≈ 50 sn). Test edilmedi.
4. **Rastgeleli + B1'de sessiz kesinti kötüleştirdi** (66 → 638 ms) ve boşaltma tepe hızı 14,7 → 24 bin mesaj/sn çıktı: bağlantılar sunucu açıldıktan sonra daha kümelenmiş geri geliyor. Olası açıklama (test edilmedi): sessiz kesintide başarısız bağlanma denemeleri hemen reddedilmek yerine zaman aşımına (≈ 5 sn) uğruyor; ağ geri gelince bekleyen SYN yeniden iletimleri eşzamanlı tamamlanıyor.
5. **Bedel:** her koşuda ≈ 35 bin cihaz tarafı gönderim hatası (cihaz kopmayı fark edene kadar yayınlar onaysız kalıyor, ardından birikime alınıyor); mesaj kaybı yok (en çok 1).

### Sınırlar ve açık noktalar

- Sessiz kesinti Docker ağından kopma ile taklit edildi (tek yönlü paket kaybı, kısmi kopma, gecikme dalgalanması yok); yalnızca B1, N = 5.000, Mosquitto, TLS'siz denendi; B2/B3, TLS ve EMQX sessiz kesintide denenmedi.
- İstemci tarafında yayın onayı zaman aşımının açık olması bir koşuldur: kapalıyken (önceki adımlar) cihazlar sessiz kesintiyi fark etmez (sonuçlar Adım 5-8 ile doğrudan karşılaştırılmaz).
- Hücre başına 3 tekrar; sürelerin kayması ve rastgeleli kuraldaki kötüleşmenin nedenleri test edilmedi.
- Toplayıcı da aynı ağdan koptuğu için ölçüm kolu kesintiyi yaşar (günlük 09'daki yeniden abone olma penceresi).
- Adım 5'in tüm sınırları geçerli.

## Sınırlar (önceden bilinen)

- Sessiz kesinti, ağdan kopmanın Docker ile taklididir (tek yönlü paket kaybı, kısmi kopma, gecikme dalgalanması yok).
- Bu bütçede yalnızca B1 ve iki bekleme kuralı denendi; B2/B3, TLS ve EMQX sessiz kesintide denenmedi.
- Toplayıcı da aynı ağdan koptuğu için (o da bir istemci) ölçüm bağlantısı kesintiyi yaşar; toplayıcının yeniden bağlanması sunucu açıldıktan sonra yaklaşık 0,5 sn sürer (günlük 09).
- Adım 5'in tüm sınırları geçerli.
