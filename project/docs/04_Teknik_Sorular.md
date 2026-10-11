# 04 — Teknik Sorular (Yalın Dille)

Son güncelleme: 3 Ekim 2026
Bu dosya, "binlerce cihazı nasıl çalıştıracağız, laptop kaldırır mı, kesintiyi nasıl yaratacağız" sorularının cevabıdır.
**Sayılar tahmindir.** Küçük bir deneme (pilot) bunları doğrular ya da değiştirir. Henüz kod yazılmadı, hiçbir şey kurulmadı.

---

## 1. "Sunucu" derken ne kastediyoruz?

- **Kiralık bir bilgisayar değil.** Cihazların bağlandığı merkez **yazılımı.**
- Laptopun içinde, bir kapta (Docker) çalışan **ücretsiz, açık kaynak bir program.**
- Sanal cihazlar da aynı laptopta başka kaplarda çalışır. İkisi aynı makinede konuşur.
- **Maliyet sıfır.** Yazılımları indirmek dışında internet de gerekmez.

```
        Senin laptopun
 ┌──────────────────────────────────────┐
 │  [Sunucu programı]  ◄──►  [Sanal cihazlar]
 │   (küçük sabit pay)       (binlercesi)
 └──────────────────────────────────────┘
```

"Sunucuya küçük bir pay veririz" demek: o programın laptopun işlemcisinden ve belleğinden **ne kadarını kullanabileceğine** sınır koymak. Bunu bilerek, deneyi kontrol edebilmek için yapıyoruz.

---

## 2. Binlerce cihaz nasıl çalışacak?

- Cihazlar **gerçek değil, yazılım parçaları.**
- Sanal bir cihaz, sunucuyla açık bir telefon hattını tutan ve arada bir mesaj söyleyen küçük bir görev.
- Çoğu zaman **bekliyor**, çok az iş yapıyor. Bu yüzden tek bir program binlercesini taşıyabilir.
- Hepsini tek kaba koymayız: bir adresten açılabilecek bağlantı sayısının sınırı var (Linux'ta yaklaşık 28 bin, TAHMİN değil belgeli). Sanal cihazları birkaç kaba böleriz, sunucu ayrı bir kapta çalışır.

---

## 3. Laptop çökmez mi?

**Çökmesini beklemiyoruz ama donabilir.** Asıl tehlike çökme değil, **ölçümün bozulması.**

- **5 bin bağlantı** bu makine için küçük bir iş. Boşta 20-60 bin bağlantının rahat olduğunu tahmin ediyoruz (**kaynaklı rakam yok**).
- **Zor an**, herkes aynı anda bağlanırken gelir. Güvenli bağlantı kurmak işlemciyi yorar.
- **Önlemler:**
  - Docker'a sabit bellek ve çekirdek sınırı koyarız, Windows'un kendisi boğulmaz.
  - 1.000 → 2.000 → 5.000 diye **küçükten büyütürüz.**
  - Sonuçlar dosyaya yazılır. Donarsa kapat-aç yapılır, kaybolan olmaz.
- **RTX 3060** bu projede hiç kullanılmaz.

---

## 4. Performans yeter mi?

**Bilmiyoruz, küçük bir deneme gösterecek.** Ama tasarımın bir avantajı var.

Biz **bilgisayarı değil, sunucuyu zorlamak** istiyoruz. Sunucuya bilerek küçük, sabit bir pay veririz (örn. 2 çekirdek, 2 GB). Küçük sunucu 5 bin cihazla bile zorlanır, yani laptopun gücü yetmese bile fırtına çıkar.

**Tersi tehlikeli:** **Yük üreten kısım** yetişemezse ölçtüğümüz şey sunucu değil, kendi yetersizliğimiz olur. Önlem:
- Yük üretici işlemcinin %80'inin altında kalır.
- Kesintisiz "boş deney" ile taban ölçülür.

**Laptopun kendisinden gelen gürültü:**
- Isınınca yavaşlar ve ölçümler tutarsızlaşır. Priz takılı, "En iyi performans" modu, her çalıştırma benzer sıcaklıkta başlar.
- Arka plandaki antivirüs ve güncellemeler ölçümü bozar.
- **Sonuçlar "bu küçük sunucuda göreceli"** olarak raporlanır. "30 bin cihazı kaldırır" gibi mutlak sayı vermeyiz.

### Gerçekçi beklenti (TAHMİN)

| Cihaz sayısı | Beklenti |
|---|---|
| 5.000 | Rahat olmalı |
| 15.000 | Büyük olasılıkla yapılır |
| 30.000 | Sınırda; çekirdek bölüşümüne ve Docker'ın bellek ayarına bağlı |

**Fırtına hiç çıkmazsa** (bağlanma çok ucuz kalırsa) tasarım değişir. Güvenli bağlantıyı (TLS) açık tutup test ederiz. Bu risk `..\..\14_Proje_Yeniden_Baglanma.md` dosyasında kayıtlı.

---

## 5. VPS (kiralık sunucu) işe yarar mı?

**Zorunlu değil, ama ileride işe yarayabilir.** Ana deneyi laptopta başlatıyoruz.

| | VPS'in etkisi |
|---|---|
| **Artı** | Sunucu ayrı makinede, sanal cihazlar laptopta olursa ikisi aynı işlemciyi paylaşmaz. Ölçümün en büyük riski azalır |
| **Eksi** | İnternet gecikmesi ve VPS'in paylaşımlı çekirdeği (başka müşterilerle) ölçüme gürültü katar |
| **Plan** | Ana deney laptopta. Sonra VPS'te **bir kez daha tekrarlanır** ve sonuçlar karşılaştırılır (planda "eklenti E4: dış geçerlilik") |

Başta VPS ile başlarsak "neyin neye yol açtığını" ayırt etmek zorlaşır.

---

## 6. Kesinti nasıl yaratılır?

Kesintinin **iki türü** var. Cihazların bunu nasıl fark ettiği çok farklı.

### Tür 1 — Temiz kesinti (sunucu programı kapanır)

- **Mağaza benzetmesi:** Mağaza kapanırken içerideki herkese "kapatıyoruz" denir ve hepsi aynı anda dışarı çıkar.
- **Cihazlar:** Bağlantının koptuğunu **anında** öğrenirler. Hepsi aynı saniyede "yeniden dene" moduna geçer.
- **Beklenti:** Çok **eş zamanlı**, yani en kötü izdiham.
- **Nasıl yaratılır:** Sunucunun kabını durdurup 60 saniye sonra yeniden başlatırız.

### Tür 2 — Sessiz kesinti (ağ kopar, kimse haber vermez)

- **Telefon hattı benzetmesi:** Hat sessizce kesiliyor. Cihazlar "cevap gelmiyor" diye kendi saatlerinde fark ediyor.
- **Cihazlar:** Her biri **farklı anda** anlar. Ama düzenli yoklama aralıkları (bizde 10 sn) yüzünden fark edişler yine dalgalar hâlinde olabilir.
- **Beklenti:** Daha dağınık görünür, ama sunucu dönünce ne olacağı **belirsiz.** Hangisinin daha kötü olduğunu önceden bilmiyoruz.
- **Nasıl yaratılır:** Sunucu kabını ağdan çıkarıp 60 saniye sonra geri bağlarız.

### Tür 3 — Planlı kapatma (eklenti)

Sunucu önce cihazlara "gidiyorum" der. Yalnızca açıklayıcı şekil için, zaman kalırsa.

### Karar (3 Ekim): **iki türü de yapıyoruz, iki kademede**

| Kademe | Kesinti türü | Kapsam | Çalıştırma |
|---|---|---|---|
| **1 (çekirdek)** | Temiz | Tüm deney (5.000 ve 15.000 cihaz) + kontroller | 140 |
| **2** | Sessiz | Yalnızca 5.000 cihaz, aynı strateji karşılaştırması | 60 |

- **Toplam ~200 çalıştırma, ~23 saat makine** (TAHMİN).
- Bir çalıştırmada **tek tür** olur, ikisi karıştırılmaz.
- Kesinti türü karşılaştırması **yalnızca 5.000 cihazda** yapılır.
- **Neden iki kademe?** Sessiz kesintinin gerçekten sessiz olduğunu henüz kanıtlamadık. Çekirdeği buna bağlamak riskli. Kademe 1 tek başına tam bir projedir.
- **Zaman yetmezse:** Kademe 2 ilk kesilir.
- **Pilotta sessiz kesinti doğrulanamazsa:** Kademe 2 iptal, yalnızca temiz kesinti kalır, raporda yazılır.

### İki incelik

1. **Çok kısa kesinti hiç fark edilmeyebilir.** Sessiz kesinti birkaç saniye sürerse bağlantılar hayatta kalır, cihazlar bir şey olduğunu anlamaz, izdiham da olmaz. Bu yüzden kesintiyi **60 saniye** seçtik (yoklama aralığının çok üstünde).
2. **"Ağdan çıkarınca sessiz kesinti olur" bir çıkarım, kanıtlanmış bilgi değil.** Denemeden varsaymayacağız. Her kesinti türünü, **ağ trafiğine bakarak** gerçekten istediğimiz türde olduğunu doğrulayacağız. Etiketine uymayan kesintiyle yapılan deney geçersiz olur.

---

## 7. Sıradaki teknik konular (henüz konuşulmadı)

- **Hangi sunucu programı?** İki aday var: **Mosquitto** (hafif, tek iş parçacıklı) ve **EMQX** (çok çekirdekli, ağır). Küçük pilotla seçilir.
- **Sanal cihazın dili:** Öneri .NET (senin yığının). Senin onayın bekleniyor.
- **Ölçümün nasıl yapıldığı:** Gecikme "planlanan gönderim zamanından" ölçülür, yoksa yavaş örnekler kaybolur.
- **Pilot testin içeriği:** Bir saatlik "tuzak" testi (1.000 → 5.000 cihaz). Onayın olmadan başlamıyoruz.
