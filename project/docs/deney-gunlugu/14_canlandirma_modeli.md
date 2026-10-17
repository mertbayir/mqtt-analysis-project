# 14 — Canlı simülasyon modeli (web arayüzü)

Tarih: 17 Ekim 2026
Durum: **Tamamlandı** (model, kalibrasyon, arayüz; 28/38 ölçülmüş hücre kabul ölçütünü sağlıyor)
Rapor bölümü: Makaleye girmez; sunum aracı (web arayüzü "Canlı simülasyon" bölümü)

## Amaç

Web sonuç gezgininde kayıtlı koşuları tekrar oynatmanın yanına, **kullanıcının sunucuyu aniden kesip açabildiği ve parametreleri değiştirebildiği** etkileşimli bir simülasyon eklemek. Sonuçların bizim ölçümlerimize göre dönmesi için basit bir sunucu modeli ölçümlere **kalibre edildi**. Bu bir **modeldir, ölçüm değildir**; arayüzde "MODEL, ÖLÇÜM DEĞİL" şeridi, ölçülmüş senaryoda ölçümle yan yana kutu ve sapma rozeti, ölçülmemiş kombinasyonda "tahmin" rozeti vardır.

## Model (`web/model.js`, aynı kod tarayıcıda ve Node'da çalışır)

- **Cihazlar (N):** durumlar {bağlı, bekliyor, deniyor, ilk bağlanma bekleme}. Bekleme kuralları `Device.cs` ile aynı (sabit 1 sn; üstel 1, 2, 4, 8, 16, 30 sn; rastgeleli üstel: ilk U(0,5), sonra U(0,5W, W)); her denemenin 5 sn zaman aşımı; ilk bağlanma 20 sn'ye yayılı; her cihaz vital 0,5/sn ve alarm 0,1/sn üretir; kesintide biriktirir.
- **Sunucu:** zaman adımlı (dt = 50 ms) akışkan kuyruk. İki iş sınıfı: **mesaj/eşitleme kuyruğu** (`W`, FIFO; gecikme = W / etkin hız) ve **bağlantı/el sıkışma işi** (`H`). El sıkışma varken CPU'nun `pay` kadarı mesajlara kalır (iki sınıflı paylaşım). Zaman aşımına uğrayan el sıkışmanın işinin `bayat` oranı kuyrukta kalır (ikinci dalga ve metastabilite doğal çıkar). N > 5.000'de mesaj maliyeti `1 + gam × (N − 5.000) / 5.000` kat artar.
- **Politikalar:** B0 at; B1 sync sonrası tümü hemen; B2 U(0,W) gecikme + sync + aralıklı boşaltma; B3 alarmlar bağlanır bağlanmaz, vital B2 gibi. Eşitleme iki geçiş (istek + yanıt), 10 sn zaman aşımı.
- **Çıktılar (saniyelik):** bağlı sayısı, deneme/sn, canlı alarm p99 (= kuyruk gecikmesinin tepesi + taban), sunucu yükü, kuyruk gecikmesi, biriken alarmın yaşı (p50, en büyük), boşaltma bitişi, eşitleme.
- **Modellenmeyenler:** mesaj kaybı, rastgele aykırı koşular, bloklar arası değişkenlik, ağ gecikmesi, bellek, sunucu içi ayrıntılar.

## Kalibrasyon (`scripts/model_kalibrasyon.mjs`)

Hedefler: `web/veri.js` içindeki ölçüm medyanları (38 hücre: 6 bağlanma, 32 bağlantı sonrası). Sunucu başına koordinat inişi (çarpımsal adım); kayıp = tepe için log hata + >100 ms süre + boşaltma bitişi + biriken alarm yaşı (bağlanma hücrelerinde TTR100, TTR50, başarısız bağlantı). Mosquitto'nun N = 5.000 hücrelerine 2,5 kat ağırlık verildi (sunumda en çok kullanılan hücreler). Parametreler:

| Sunucu | m (sn/mesaj) | cConn | h (TLS) | cSync | bayat | L0 | L0s | pay | gam |
|---|---|---|---|---|---|---|---|---|---|
| mosquitto | 3.8e-05 | 0.0001391 | 0.001231 | 0.00039 | 0.4182 | 0.02491 | 0.072 | 0.22 | 0.5077 |
| emqx2 | 1.703e-05 | 0.0001106 | 0.0016 | 0.0004 | 0.5 | 0.04576 | 0.07475 | 0.1 | 0.3 |
| emqx4 | 3e-05 | 0.0001625 | 0.0016 | 0.0002609 | 0.5 | 0.04048 | 0.0575 | 0.1 | 0.3 |

Not: EMQX'te TLS denenmediği için `h` kalibre edilmedi (arayüzde EMQX için TLS kapalı). EMQX parametreleri yalnızca N = 5.000, TLS'siz hücrelerden uydurulmuştur.

## Model vs ölçüm

Kabul ölçütü: bağlantı sonrası hücrelerde tepe 2 kat içinde **ve** boşaltma bitişi ±%25; bağlanma hücrelerinde TTR100 1,5 kat içinde. **28/38 hücre sağlıyor** (hedef ≥ %70: sağlandı).

**Bağlantı sonrası (alarm gecikmesi tepesi, ms):**

| Sunucu | N | Kural / B | Ölçüm | Model | Oran | Boşaltma (ölçüm / model, sn) | Tuttu |
|---|---|---|---|---|---|---|---|
| Mosquitto | 5.000 | ustel-rastgele / b1 | 51 | 234 | 4,10 | 29 / 33 | **hayır** |
| Mosquitto | 5.000 | ustel / b1 | 9.158 | 9.263 | 1,01 | 43 / 46 | evet |
| Mosquitto | 5.000 | ustel-rastgele / b0 | 60 | 62 | 1,03 | – / – | evet |
| Mosquitto | 5.000 | ustel / b2 | 143 | 88 | 0,64 | 66 / 67 | evet |
| Mosquitto | 5.000 | ustel-rastgele / b2 | 79 | 62 | 0,80 | 64 / 70 | evet |
| Mosquitto | 5.000 | ustel / b0 | 47 | 62 | 1,27 | – / – | evet |
| Mosquitto | 10.000 | ustel / b1 | 19.741 | 25.581 | 1,30 | 61 / 72 | evet |
| Mosquitto | 10.000 | ustel-rastgele / b2 | 7.324 | 3.440 | 0,47 | 71 / 74 | **hayır** |
| Mosquitto | 10.000 | ustel / b2 | 9.259 | 6.731 | 0,73 | 80 / 84 | evet |
| Mosquitto | 10.000 | ustel-rastgele / b1 | 6.290 | 9.302 | 1,48 | 34 / 63 | **hayır** |
| Mosquitto | 10.000 | ustel-rastgele / b0 | 90 | 98 | 1,09 | – / – | evet |
| Mosquitto | 10.000 | ustel-rastgele / b2-w90i500 | 276 | 98 | 0,37 | 140 / 147 | **hayır** |
| Mosquitto | 10.000 | ustel-rastgele / b2-w60i1000 | 237 | 98 | 0,43 | 138 / 147 | **hayır** |
| Mosquitto TLS | 5.000 | ustel / b1 | 8.234 | 9.944 | 1,21 | 69 / 70 | evet |
| Mosquitto TLS | 5.000 | ustel / b0 | 613 | 62 | 0,11 | – / – | **hayır** |
| Mosquitto TLS | 5.000 | ustel-rastgele / b0 | 63 | 62 | 0,98 | – / – | evet |
| Mosquitto TLS | 5.000 | ustel-rastgele / b2 | 112 | 62 | 0,58 | 68 / 70 | evet |
| Mosquitto TLS | 5.000 | ustel-rastgele / b1 | 1.234 | 1.864 | 1,51 | 30 / 34 | evet |
| Mosquitto TLS | 5.000 | ustel / b2 | 790 | 718 | 0,91 | 108 / 108 | evet |
| EMQX 2 CPU | 5.000 | ustel / b2 | 186 | 197 | 1,06 | 59 / 67 | evet |
| EMQX 2 CPU | 5.000 | ustel-rastgele / b2 | 163 | 84 | 0,54 | 71 / 70 | evet |
| EMQX 2 CPU | 5.000 | ustel-rastgele / b0 | 66 | 84 | 1,25 | – / – | evet |
| EMQX 2 CPU | 5.000 | ustel-rastgele / b1 | 93 | 84 | 0,91 | 32 / 33 | evet |
| EMQX 2 CPU | 5.000 | ustel / b0 | 112 | 84 | 0,77 | – / – | evet |
| EMQX 2 CPU | 5.000 | ustel / b1 | 16.188 | 19.431 | 1,20 | 49 / 47 | evet |
| EMQX 4 CPU | 5.000 | ustel / b2 | 161 | 203 | 1,25 | 59 / 67 | evet |
| EMQX 4 CPU | 5.000 | ustel-rastgele / b1 | 127 | 118 | 0,93 | 32 / 33 | evet |
| EMQX 4 CPU | 5.000 | ustel-rastgele / b2 | 316 | 70 | 0,24 | 71 / 70 | **hayır** |
| EMQX 4 CPU | 5.000 | ustel / b1 | 11.527 | 13.029 | 1,13 | 39 / 47 | evet |
| Mosquitto | 10.000 | ustel-rastgele / b3-w90i500 | 210 | 98 | 0,49 | 136 / 143 | **hayır** |
| Mosquitto TLS | 5.000 | ustel-rastgele / b3 | 87 | 62 | 0,74 | 64 / 68 | evet |
| Mosquitto TLS | 5.000 | ustel / b3 | 1.524 | 3.083 | 2,02 | 106 / 105 | **hayır** |

**Yalnızca bağlanma:**

| Sunucu | N | Kural | TTR100 (ölçüm / model, sn) | TTR50 | Başarısız (ölçüm / model) | Tuttu |
|---|---|---|---|---|---|---|
| Mosquitto TLS | 2.000 | ustel | 61,2 / 29,0 | 26,2 / 28,0 | 325 / 0 | **hayır** |
| Mosquitto TLS | 2.000 | sabit | 3,2 / 4,0 | 2,1 / 3,0 | 670 / 0 | evet |
| Mosquitto TLS | 5.000 | sabit | 9,9 / 9,0 | 3,9 / 5,0 | 1.128 / 1.351 | evet |
| Mosquitto TLS | 5.000 | ustel | 62,9 / 63,0 | 28,0 / 30,0 | 1.836 / 1.351 | evet |
| Mosquitto TLS | 2.000 | ustel-rastgele | 28,1 / 32,0 | 12,0 / 13,0 | 0 / 5 | evet |
| Mosquitto TLS | 5.000 | ustel-rastgele | 28,9 / 33,0 | 10,9 / 13,0 | 0 / 14 | evet |

## Tutmayan hücreler ve yorum

- **Rastgeleli + B1, N = 5.000, TLS'siz** (ölçüm 51, model 234 ms): model bu hücrede kuyruğu abartıyor (ölçüm 100 ms eşiğini hiç aşmadı). Arayüzde "modelde sapma var" rozeti görünür.
- **N = 10.000 B2 hücreleri** (30/200: ölçüm 7.324 / model 3.440; 90/500 ve 60/1000: ölçüm 237-276 / model 98) ve **B3 90/500** (210 / 98): model N = 10.000'deki doymanın başlangıcını tam yakalayamıyor; küçük mutlak değerlerde model tabana (≈ 100 ms) iniyor.
- **TLS, üstel, B0** (ölçüm 613, model 62 ms): el sıkışma fırtınasının canlı mesajlara yansıması modelde yok denecek kadar küçük.
- **TLS, üstel, B3** (ölçüm 1.524, model 3.083 ms) ve **EMQX 4 CPU rastgeleli B2** (ölçümde aykırı koşu medyanı etkiliyor): sapmalı.
- **TLS, N = 2.000, üstel** (TTR100 ölçüm 61, model 29 sn): modelde ikinci dalga yok; gerçekte N = 2.000'de de var (günlük 10, HT5). Mekanizması bilinmiyor.
- Başlıca (sunumda en çok gösterilen) hücreler iyi tutuyor: Mosquitto üstel + B1 (ölçüm 9.158, model 9.263), N = 10.000 üstel + B1 (19.741 / 25.581), TLS üstel + B1 (8.234 / 9.944), TLS bağlanma TTR100 (62,9 / 63), EMQX üstel + B1 (16.188 / 19.431) ve B2'nin üstelde sağladığı çözüm.

## Dosyalar

`web/model.js`, `scripts/model_kalibrasyon.mjs`, `web/model_param.json`, `web/model_artik.json` (hücre başına model/ölçüm), `web/sablon.html` (arayüz bölümü "Canlı simülasyon"), `scripts/web_uret.py` (modeli ve parametreleri tek dosyaya gömer). Yeniden üretim: `node scripts/model_kalibrasyon.mjs mosquitto|emqx2|emqx4`, `node scripts/model_kalibrasyon.mjs birlestir`, `python scripts/web_uret.py`.

## Sınırlar

- Model basit bir yapıdır; parametreleri 3 sunucu için uydurulmuştur, mekanik doğruluk iddiası taşımaz.
- Ölçülmemiş kombinasyonlar (ör. 7.500 cihaz, 30 sn kesinti, EMQX + TLS) tahmindir.
- Kalibrasyon hedefleri, farklı bloklardan gelen ölçüm medyanlarıdır (bloklar arası değişkenlik büyüktür, günlük 13); tek bir ölçüme "tam uyum" beklenmemelidir.
- Rastgele aykırı koşular ve mesaj kaybı modellenmedi.
