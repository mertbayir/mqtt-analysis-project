# 03 — Biz Ekstra Ne Yapıyoruz?

## Küçük katkılar

| # | Katkı | Anlamı |
|---|---|---|
| 1 | **İki aşamayı ayrı ölçmek** | "Geri dönme" ile "döndükten sonraki işler" (öğrenme + teslim) ayrı ayrı ölçülür. Çoğu çalışma yalnızca ilkine bakıyor |
| 2 | **Döndükten sonraki işi ayrı zamanlamak** | Bekleyip mi yapsın, hemen mi? Canlı mesajlara ne kadar zarar verir, toparlanmaya ne kadar bedel öder? |
| 3 | **Açık, tekrarlanabilir test düzeneği** | Başkası aynı deneyi aynı şekilde çalıştırabilir. Seneye tez için de temel olur |
| 4 | **Kesinti türüne bakmak** (ikincil) | Aynı denemeyi hem temiz hem sessiz kesintiyle yapıp sonucun değişip değişmediğine bakarız (yalnızca 5.000 cihazda). Hangisinin daha kötü olduğunu önceden bilmiyoruz |

## Ne tür bir katkı bu?

Bu bir **ölçüm çalışmasıdır**, yeni bir yöntem değil.
- Bilinen stratejiler (rastgele bekleme gibi) **karşılaştırma tabanı** olarak kullanılır.
- Yenilik, **bilinen şeylerin yeni bir ortamda ve yeni bir bileşimle ölçülmesinde.**

Küçük özgünlüğün dört yolundan hepsi bizde var:

| Yol | Bizde |
|---|---|
| Bilinen şey, yeni ortam | Uygulama katmanında, cihaz filosu (önceki çalışmalar telefon şebekesinde ya da tek cihazla) |
| Bilinen iki şeyin birlikte ölçümü | Bağlanma + bağlantı sonrası yük |
| Denenmemiş değişken | Çok cihazın **aynı anda** boşaltması (2024 çalışması gelecek çalışma bırakmış) |
| Ölçülmemiş iddia | Standardın "şu kadar sonra gel" alanı taşımamasının etkisi |

## Sunumda söylenecek dürüst cümle

> "Yeni bir algoritma sunmuyoruz. Bilinen stratejileri, bağlantı sonrası yükü ayrı hesaba katarak MQTT 5 üzerinde ölçüyoruz. Bildiğimiz kadarıyla bu ölçüm açıkça yayımlanmamış; en yakın çalışmalar şunlar ve farkımız şu."

## Söylemeyeceklerimiz

- "İlk kez biz yaptık", "literatürde yok" (yalnızca: **"bulamadık"**).
- "Rastgele bekleme işe yarar" bir **bulgu** olarak (bilinen şey; ortamın doğru çalıştığının sağlaması).
- Tek dizüstünden çıkan **mutlak** sayılar ("sunucu 30 bin cihazı kaldırır").

## Kapsam dışı (bilerek)

- Yeni bir algoritma icat etmek
- Kendi kendine öğrenen / uyarlanan stratejiler
- Pil enerjisi ölçümü
- Çok sunuculu ya da katmanlı sistemler
- Gerçek donanım
- Akıllı şehir uygulaması (2209 projesinden uzak duruyoruz)

## Başarı ölçütü

- Çekirdek sorular ölçülür ve rapor edilir.
- **Hipotez çürürse de başarılıdır.** "Bu ölçekte ikinci izdiham çıkmadı" da bir bulgudur.
- Test düzeneği başkası tarafından çalıştırılabilir durumdadır.
