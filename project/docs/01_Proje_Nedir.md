# 01 — Proje Nedir?

## Başlığımız

> **Çok Cihazlı MQTT 5 Sistemlerinde Toplu Yeniden Bağlanma ve Bağlantı Sonrası Yük: Bir Ölçüm Ortamı ve Strateji Karşılaştırması**

Sunum türü: **Proje**.

### Başlık ne demek?

| Parça | Anlamı |
|---|---|
| **Çok cihazlı** | Binlerce cihaz (sensör, tablet, kiosk) tek bir merkezi sunucuya bağlı |
| **MQTT 5** | Cihazlarla sunucunun konuştuğu yaygın bir ortak dil. Konunun özü değil, yalnızca seçtiğimiz zemin |
| **Toplu yeniden bağlanma** | Sunucu ya da ağ kesilip geri gelince cihazların hepsi aynı anda geri dönmeye çalışır |
| **Bağlantı sonrası yük** | Geri dönen cihazların ardından yaptığı işler |
| **Ölçüm ortamı** | Bunu deneyebileceğimiz, tekrar tekrar çalıştırabileceğimiz bir test düzeneği |
| **Strateji karşılaştırması** | Farklı davranış kurallarını yan yana koyup sonucu kıyaslamak |

---

## Problem, günlük hayattan

Bir mağaza bir dakikalığına kapanıyor. Kapı açılınca dışarıda bekleyen binlerce kişi aynı anda içeri girmeye çalışıyor. Kapıda izdiham oluyor. Bir kısmı içeri giremiyor, geri dönüp tekrar deniyor, izdiham uzuyor.

İçeri girenlerin işi de bitmiyor, iki işleri daha var:

1. **"Ben yokken ne değişti?"** diye öğrenmek.
2. **Ben yokken biriktirdiğim şeyleri teslim etmek.**

Binlerce kişi bunu da aynı anda yaparsa **ikinci bir izdiham** başlayabilir. İçeride normal alışveriş yapanlar (bizdeki "canlı mesajlar") da bundan etkilenir.

### Toparlanma üç aşamada olur

```
Kesinti  ──►  1) BAĞLAN  ──►  2) ÖĞREN  ──►  3) TESLİM ET  ──►  Normal
              cihaz geri       "ben yokken     "ben yokken
              gelir            ne değişti?"    biriktirdiğim
              (herkes                          mesajları
              aynı anda)                       gönder"
```

- Çoğu çalışma yalnızca **1. aşamaya** bakar: "cihazlar rastgele beklesin, kalabalık dağılsın."
- Biz **2. ve 3. aşamaya** da bakarız.

---

## Neler yapacağız?

1. Bilgisayarda **binlerce sanal cihaz** ve bir sunucudan oluşan bir test düzeneği kuracağız.
2. Sunucuyu kesip geri getireceğiz ve cihazların nasıl toparlandığını izleyeceğiz. Kesintiyi **iki türlü** yapacağız: önce **temiz** (sunucu kapanır, cihazlar anında öğrenir), sonra **sessiz** (ağ sessizce kopar). Ayrıntı: `04`.
3. Cihazların **geri dönme kurallarını** değiştirip sonucu karşılaştıracağız:
   - **Hemen mi dönsünler, rastgele bir süre bekleyip mi?**
   - Döndükten sonra **hemen mi öğrenip teslim etsinler, biraz bekleyip mi?**
4. Her denemeyi **10 kez** tekrarlayıp ortalama ve belirsizlik payıyla raporlayacağız.
5. Aynı sayıda cihazı **kesinti olmadan, yavaş yavaş** bağlayarak kontrol deneyi yapacağız. Böylece sorunun cihaz sayısından değil, **aynı anda gelmelerinden** çıktığını göstereceğiz.

---

## Neler göreceğiz?

Grafiklerle şunları:

- Herkesin normale dönmesi **ne kadar sürdü?**
- Kaç bağlantı **başarısız oldu?**
- Toparlanma sürerken **canlı mesajlar ne kadar gecikti?**
- Cihaz sayısı arttıkça sistem **yavaş yavaş mı bozuldu, yoksa bir noktada birden mi çöktü?** (devrilme noktası)
- Sonuç **kesintinin türüne** (temiz / sessiz) göre değişti mi? (Yalnızca 5.000 cihazda bakılır. Hangisinin daha kötü olacağını önceden bilmiyoruz.)

### Sonuçları bilmiyoruz. İki ihtimal var

| İhtimal | Anlamı |
|---|---|
| **İkinci izdiham var** | Döndükten sonra hemen öğrenip teslim etmek, canlı akışı yavaşlatıyor. Bekleyip yapmak canlı akışı kurtarıyor ama toparlanmayı biraz uzatıyor. Bu bir **dengeleme** bulgusu |
| **İkinci izdiham yok** | "Bu ölçekte bağlantı sonrası yük sorun çıkarmadı." Bu da bir bulgu |

İkisi de rapor edilebilir. **Sonucu önceden bildiğimizi iddia etmiyoruz.**

---

## Elimizde ne olacak?

- Tekrar çalıştırılabilir **bir test düzeneği** (kod + açıklama)
- **Grafikler ve bir rapor**
- Sunum
- Seneye tez için **temel** (örn. sunucunun cihazlara "şu kadar sonra gel" demesi, birden fazla sunucu)
