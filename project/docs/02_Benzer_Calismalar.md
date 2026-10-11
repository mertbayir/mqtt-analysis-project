# 02 — Daha Önce Yapılanlar (Dürüst Liste)

> **Uyarı:** Bu liste bir yapay zekâ asistanıyla yapılan taramaya dayanıyor. Bazı kaynaklar tam okundu, bazıları yalnızca özet düzeyinde. **Künyeleri ve cümleleri kendin doğrula** (ayrıntı: `..\15b_Dogrudan_Onceki_Calismalar.md`, `..\15c_Ozgunluk_Acilari.md`).
> **"Bulamadık", "yok" demek değildir.** Teslimden önce bir kez daha tarama yapılacak.

---

## Kısa tablo

| Çalışma | Ne yapmış | Bizden farkı |
|---|---|---|
| **Eski internet standartları** (2008 ve 2011) | "Herkes aynı anda gelmesin, rastgele bekleyin" diye öneriyor | Bu **bilinen** şey. Yeni bulgu diye sunmuyoruz, **karşılaştırma tabanı** olarak kullanıyoruz |
| **Çöküş araştırmaları** (2022, 2025) | Tekrar deneme izdihamının sistemi çökertebileceğini göstermiş | Genel konu. Cihaz filolarına özel, kesinti sonrası ölçüm değil |
| **Spillway** (2026) | Telefon şebekesi merkezinde benzer izdihamları incelemiş | **En yakın çalışma. Henüz okunmadı.** Okuyunca farkımızı netleştireceğiz |
| **Biriken mesajları boşaltma** (2024) | **Tek** bir cihazın biriktirdiğini boşaltmasını ölçmüş. "Çok cihaz aynı anda boşaltırsa?" sorusunu gelecek çalışma olarak bırakmış | Bizim ek kısmımız tam bu boşluğa denk geliyor |

---

## Ayrıntı

### 1. Eski internet standartları
- **RFC 5390** (2008, telefon görüşme protokolü SIP): Olguya "çığ yeniden başlatma" diyor. Şehirde elektrik gelince tüm cihazların aynı anda bağlanmaya çalışması senaryosunu anlatıyor.
- **RFC 6455** (2011, WebSocket): Yeniden bağlanmanın sunucuyu boğabileceğini söylüyor. **İlk bağlanmadan önce 0-5 saniye rastgele bekle, sonra bekleme süresini katlayarak artır** diyor.
- **Floyd ve Jacobson** (1994): Aynı anda başlayan zamanlayıcıların kendiliğinden birlikte hareket etmesi, rastgelelik ihtiyacı.
- **Sonuç:** "Rastgele beklemek işe yarar" **yeni değil.** Biz bunu bulgu diye sunmayacağız.

### 2. Çöküş araştırmaları
- **Huang ve ark.** (OSDI 2022), **Isaacs** (HotOS 2025): Bir sistem aşırı yüklenince başarısız istekler tekrar denenir, yük artar, sistem kendi kendini çökertir.
- Genel sistem araştırmaları. Cihaz filolarının kesinti sonrası davranışını ayrı ölçmezler.

### 3. Spillway (2026)
- Telefon şebekesi merkezinde (çekirdek ağ) aynı tür izdihamlar.
- Bizim ortam: kendi sunucumuz ve sanal cihazlar. **Aynı problem ailesi, farklı ortam.**
- **Okumadan karşılaştırma yapamıyoruz.** Bu yüzden form öncesi yapılacak tek zorunlu iş.

### 4. Biriken mesajları boşaltma
- **Domingues, Faria, Portugal** (EURASIP JWCN 2024): Tek cihazın biriktirdiği mesajları sunucuya boşaltması. Canlı verinin etkisi küçük çıkmış. Aynı anda çok cihaz boşaltmayı **gelecek çalışma** bırakmış.
- Bu cümle, **bu projenin en iyi desteklenen boşluğu.** Bir ajan okudu, **sen de doğrula.**

---

## Standartta bir boşluk var (kontrol edilebilir)

MQTT 5 standardında, sunucunun reddettiği cihaza **"şu kadar saniye sonra gel"** diyebileceği bir alan **bulunamadı.** Ret nedenleri var (örn. "sunucu meşgul"), ama **ne zaman tekrar deneyeceğini** söyleyen bir alan yok. Bunun pratikte ne anlama geldiğini ölçen yayın da bulamadık.
Bu bir **gözlem**, iddia değil: standart metninden doğrulanabilir.

---

## Riskler (dürüstçe)

- Alanda 2025-2026'da yeni ön baskılar çıkıyor. **Teslimden önce arXiv'i yeniden tara.**
- Bir çalışma bu konuyu daha uzun bir makalenin içinde bir bölüm olarak ele almış olabilir; hızlı aramada görünmeyebilir.
- Ücretli ve erişilemeyen bazı makaleler (ACM, IEEE) yalnızca özet düzeyinde incelendi.


---

## Ek (11 Ekim): Tıbbi IoT (IoMT) hızlı taraması

> **Uyarı:** Üç web aramasının özetine dayanıyor. Özetler hata içerebilir, makaleleri **tam okumadım.** Künyeleri ve sayıları kendin doğrula. "Bulunamadı", "yok" demek değildir.

| Bulgu | Not |
|---|---|
| **Domingues ve ark. (EURASIP 2024)** arama özetine göre **dijital sağlık senaryosunda**: bağlantı kopunca sensör düğümleri eksik veriyi MQTT ile yerel sunucuya geri gönderiyor; asıl soru, mesaj başına en uygun veri boyutu | Bizim en yakın dayanağımız zaten sağlık çerçevesinde. Tıbbi çerçeve bu yüzden **doğal** ama **tek başına özgünlük kazandırmaz** |
| **Brezilya'dan bir çalıştay makalesi** (Portekizce, "Tıbbi IoT sistemlerinde çok bulutlu mimaride iletişim performansı ve güvenilirliği"; yıl teyit edilmedi) | Hasta izleme için MQTT, hata enjeksiyonlu denemeler; gecikme, kullanılabilirlik, yük devri süresi ölçülüyor. **Çerçevemize en yakın yeni bulgu. Okunmalı** |
| **2026 endüstriyel MQTT ön baskısı** (preprints.org 202608.1610) | Arama özeti: rastgele beklemeli üstel geri çekilme tepe bağlantı denemesini 237,3'ten 57,0'a (100 ms'de) düşürmüş, ama %95'lik yeniden bağlanma süresini 0,13 sn'den 0,93 sn'ye çıkarmış; testbed aynı makinede. 15b'deki "2026 oyuncak ölçek jitter karşılaştırması" riskiyle aynı olabilir, **teyit edilmedi** |
| Klinik ortamda **alarm gecikmesini toplu yeniden bağlanma sırasında ölçen** çalışma | Aramada **bulunamadı** (zayıf kanıt; yalnızca özetler tarandı) |
| Cezayir tezi (ActiveMQ ana/yedek sunucu, sağlık) | Kesintisiz yük devri iddiası, **fırtına ölçümü yok** (özetten) |

**Metodoloji notu (aramadan):** Biriken mesajlar canlı gecikmeyi gizler (bir saat çevrimdışı abone, bir saatlik "gecikme" görür). Bu yüzden **canlı teslimat ile biriken mesaj boşaltması ayrı ölçülmeli**, bizim tasarımla uyumlu.
