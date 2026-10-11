# Proje Dokümanları — Toplu Yeniden Bağlanma

Son güncelleme: 3 Ekim 2026
Durum: **Konu kilitlendi.** Henüz hiçbir kod yazılmadı, hiçbir şey kurulmadı.

## Bu klasör ne için?

Ders: *Bilimsel Araştırma, Proje Hazırlama ve Seminer* (Sakarya Üniversitesi, yüksek lisans).
Bu klasör, seçtiğimiz proje konusunun **yalın dille** anlatıldığı yerdir. Yazılım bilmeyen biri de okuyup ne yaptığımızı anlayabilmeli.

Daha eski ve daha uzun araştırma notları bir üst klasörde duruyor (`..\00_OKU_BENI.md`). Orada form metni, plan ve tüm kaynak tartışması var. Bu klasör onların **sadeleştirilmiş, projeye özel** halidir.

## Dosyalar (bu sırayla oku)

| Dosya | İçeriği | Süre |
|---|---|---|
| `01_Proje_Nedir.md` | Başlık, problem, ne yapacağız, neler göreceğiz | 5 dk |
| `02_Benzer_Calismalar.md` | Daha önce yapılanlar ve bizden farkı (dürüst liste) | 5 dk |
| `03_Bizim_Katkimiz.md` | Biz ekstra ne yapıyoruz, ne iddia etmiyoruz | 3 dk |
| `04_Teknik_Sorular.md` | Sunucu nedir, binlerce cihaz nasıl çalışır, laptop kaldırır mı, VPS, **kesinti nasıl yaratılır (iki tür, iki kademe)** | 10 dk |

## Sade sözlük

| Terim | Anlamı |
|---|---|
| **Cihaz** | Sunucuyla konuşan küçük istemci: sensör, tablet, kiosk. Bizde gerçek değil, **sanal** olacak |
| **Sunucu** | Cihazların bağlandığı merkez **yazılımı.** Bizde ayrı bir makine ya da kiralık bilgisayar **değil**: laptopun içinde, bir kapta (Docker) çalışan ücretsiz, açık kaynak bir program. Maliyeti sıfır |
| **Kap (Docker konteyneri)** | Bir programı, kendi küçük kapalı alanında çalıştırma yöntemi. Sunucu bir kapta, sanal cihazlar başka kaplarda çalışır; hepsi aynı laptopta. Her kaba "şu kadar işlemci ve bellek kullanabilirsin" sınırı konabilir |
| **MQTT 5** | Cihazlarla sunucunun konuştuğu yaygın bir ortak dil. Konunun özü değil, seçtiğimiz zemin |
| **Yeniden bağlanma** | Bağlantı koptuktan sonra cihazın tekrar bağlanması |
| **Toplu yeniden bağlanma** | Binlerce cihazın aynı anda geri dönmeye çalışması |
| **Bağlantı sonrası yük** | Geri dönen cihazın ardından yaptığı işler: "ben yokken ne değişti?" diye öğrenmek, "ben yokken biriktirdiklerimi" teslim etmek |
| **Canlı mesaj** | Toparlanma sürerken akmaya devam eden normal mesajlar |
| **Toparlanma süresi** | Servis geri geldikten sonra cihazların normale dönmesi ne kadar sürdü |
| **Devrilme noktası** | Sistemin yavaş yavaş değil, bir noktada **aniden** bozulduğu cihaz sayısı |
| **Temiz kesinti** | Sunucu programı kapanır; cihazlar bağlantının koptuğunu **anında** öğrenir. En eş zamanlı izdiham. Çekirdek deneyde (Kademe 1) bu kullanılır |
| **Sessiz kesinti** | Ağ sessizce kopar, kimse haber vermez; cihazlar bunu kendi saatlerinde fark eder. Kademe 2'de, yalnızca 5.000 cihazla denenir |

## Dürüstlük notu

- Bu klasördeki "benzer çalışmalar" bir yapay zekâ asistanıyla yapılan taramaya dayanıyor. Makale künyelerini **kendin doğrula**. Ayrıntı: `..\15b_Dogrudan_Onceki_Calismalar.md`.
- "Bulamadık", "yok" demek değildir. Teslimden önce bir kez daha tarama yapılacak.
- Sayılar (saat, cihaz sayısı) **tahmindir.** Küçük bir deneme bunları doğrular veya değiştirir.
