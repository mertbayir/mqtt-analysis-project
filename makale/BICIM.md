# Makale Biçim Kararları

Şablon verilmedi, biçim bizim seçimimiz. Karar tarihi: 15 Ekim 2026. Hoca ya da ders bir şart bildirirse bu dosya güncellenir.

| Konu | Karar |
|---|---|
| Dil | Türkçe (özet ayrıca İngilizce) |
| Tür | Makale biçiminde proje raporu (Giriş, İlgili Çalışmalar, Yöntem, Bulgular, Tartışma, Sonuç) |
| Sayfa | A4, kenar boşluğu 2,5 cm, tek sütun |
| Yazı tipi | Times New Roman, gövde 12 pt, satır aralığı 1,5; başlıklar kalın (1. düzey 14 pt, 2. düzey 12 pt) |
| Kod ve komutlar | Consolas / Courier New, 10 pt |
| Şekil | Şekil altı yazısı ("Şekil 1. ..."), 300 dpi PNG ya da vektör PDF, **tüm şekiller betikle üretilir** |
| Tablo | Tablo üstü yazısı ("Tablo 1. ..."), sayılar betikten alınır |
| Kaynak gösterme | Sayısal [1], [2] (IEEE tarzı), kaynakça kullanım sırasına göre |
| Sayı yazımı | **Metinde Türkçe**: ondalık virgül (2,1 sn), binlik ayraç nokta (5.000). **Veri dosyalarında** nokta ondalık (2.1) |
| Birim | Rakamla sayı ile birim arasında boşluk (60 sn, 2 GB) |
| Kaynak dili | Kaynaklar özgün dilinde. Doğrudan alıntı yapılmaz, özetlenir; kaynak cümleleri **kendi gözümüzle doğrulanmadan** metne girmez |
| Üretim | Yazı Markdown'da (depoda), son hâl Word (.docx) ve PDF'e aktarılır |

## Yazım kuralları (bu çalışmaya özel)

1. **Hiçbir sayı elle yazılmaz.** Tablolardaki ve metindeki her sayının dayandığı koşu adı (`adim3_sabit_n5000_s1_v4ip` gibi) taslakta not edilir, ya da `scripts/ozet.ps1` çıktısından alınır.
2. **"Literatürde yok", "ilk kez" denmez.** Yalnızca "bildiğimiz kadarıyla ... bulunamadı".
3. **Sınırlar her bulguyla birlikte yazılır** (tek bilgisayar, tek koşu, TLS'siz, sanal cihaz).
4. **Tek koşu "ön gözlem" diye etiketlenir.** Nihai bulgu yalnızca tekrarlı koşulardan (medyan + aralık) gelir.
5. **Tıbbi çerçeve bir senaryodur, klinik iddia değildir.** "Sistem davranışını ölçüyoruz, klinik güvenlik iddiası yoktur."
6. **Ölçüm hataları ve düzeltmeleri raporlanır** (Bölüm 4); bu çalışmanın güvenilirlik göstergesidir.
