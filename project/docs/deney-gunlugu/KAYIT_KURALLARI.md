# Deney Kayıt Kuralları

Amaç: Makalede ve sunumda kullanılacak her sayının **hangi girdiyle, hangi kodla, hangi adımlarla** üretildiğini sonradan kanıtlayabilmek. Başkası (ya da biz, 2 ay sonra) aynı deneyi tekrarlayabilmeli.

## 1. Her deney için ne kaydedilir?

| Ne | Nerede | Kim yazar |
|---|---|---|
| **Girdiler** (cihaz sayısı, kural ve parametreleri, tohum, süre, kalp atışı, zaman aşımı, MQTT sürümü) | `results/<koşu>.girdi.json` | Simülatör, koşu başında otomatik |
| **Kod sürümü** (git commit, kodda commit edilmemiş değişiklik var mı) | `results/deney_kayitlari.csv` | Betik, otomatik |
| **Ortam** (donanım, Docker, yazılım sürümleri) | `docs/deney-gunlugu/00_ortam.md` | Elle, ortam değişince güncellenir |
| **Kesinti** (türü, durdurma ve başlatma zamanı, kapalı kalma süresi) | `results/<koşu>.olaylar.txt` + kayıt defteri | Betik, otomatik |
| **Ham çıktı** (saniyelik özet) | `results/<koşu>.csv` | Simülatör |
| **Konsol çıktısı** | `results/<koşu>.konsol.txt` | Betik |
| **Özet sayılar** (tepe, toplam, toparlanma süresi) | `scripts/ozet.ps1` → `results/ozet_adim2.txt` | Betik. **Makaledeki sayılar buradan, elle hesaplanmaz** |
| **Adımlar, gözlem, yorum, sınırlar, hatalar** | `docs/deney-gunlugu/NN_*.md` | Elle (aşağıdaki şablon) |
| **Karar ve gerekçesi** | `docs/deney-gunlugu/karar_defteri.md` | Elle |

**Koşu adı** her şeyi birbirine bağlar: `adim2_<kural>_n<cihaz>_s<tohum>`. Makalede bir sayıdan koşuya, koşudan commit'e ulaşılabilir.

## 2. Her deneyde izlenecek sıra

1. **Başlamadan önce** günlük girdisinin amacını ve (varsa) hipotezi yaz. Sonuç görülmeden önce yazılması önemli.
2. Kodu commit'le (ya da kayıt defterindeki `kod_degisikligi_var` alanının `True` olacağını bil, nedenini nota yaz).
3. Koşuyu **betikle** çalıştır (elle komut yazma, betik kaydı tutar). Gerekirse `-Not "..."` ile not düş.
4. `scripts/ozet.ps1` ile özeti üret.
5. Günlük girdisini tamamla: sonuç tablosu, yorum, **sınırlar**, beklenmedik şeyler.
6. Hata bulunursa **silme, kaydet:** ne bulundu, hangi koşular etkilendi, nasıl düzeltildi (bkz. 02, "Bulunan ve düzeltilen hatalar").
7. Commit.

## 3. Kurallar

- **Hiçbir koşu silinmez.** Hatalı koşu "geçersiz" diye işaretlenir (kayıt defterinin `not` alanı), neden yazılır.
- **Sonuç görüldükten sonra hipotez değiştirilmez.** Değişirse karar defterine tarihle yazılır.
- **Tek koşu kanıt değildir.** Tek koşu "ön gözlem" diye etiketlenir. Nitekim sabit kuralın iki tohumu (s1, s2) farklı çıktı (toplam deneme 599 ve 500).
- **Sınırlar yazılmadan sonuç yazılmaz** (ölçek, makine, tek tekrar, çözünürlük).
- **Kodun ürettiği sayıları elle yeniden hesaplama;** betik güncellenir, tüm sayılar yeniden üretilir.
- **Kaynak göstereceğimiz her cümle** (RFC, makale) doğrudan kaynaktan doğrulanır, doğrulama tarihi yazılır.

## 4. Günlük girdisi şablonu

```markdown
# NN — Adım X: <başlık>

Tarih: <gün ay yıl>
Durum: <planlandı / tamamlandı / geçersiz>
Rapor bölümü: <Yöntem / Bulgular / Tartışma>

## Amaç ve hipotez
## Girdiler (tablo: parametre, değer)
## Adımlar (komutlar ve sıra)
## Çıktılar (dosyalar, koşu adları)
## Sonuçlar (tablo)
## Yorum
## Sınırlar
## Bulunan hatalar ve düzeltmeler
## Sonraki adım
```

## 5. Bilinen eksikler (kayıt sistemi için)

- Adım 2'nin ilk üç koşusu (`s1`) geriye dönük kaydedildi, `girdi.json` dosyaları yok. Girdileri kayıt defterinde ve `03_adim2b` günlüğünde.
- Adım 2'nin en ilk koşuları (tek cihaz ve sayacı hatalı 100 cihaz koşusu) git geçmişinde (`412f4d2`), `results/` altında değil. Sayaç hatalı olduğu için analizde **kullanılmaz.**
- Docker/WSL kaynak sınırı (`.wslconfig`) henüz açık yazılmadı. Büyük cihaz sayısından önce yazılacak ve `00_ortam.md`'ye eklenecek.

## 6. Bilinen tuzaklar (yaşandı)

- **Türkçe karakterli dosyayı PowerShell 5.1 ile düzenlerken** `Get-Content` kodlamayı ANSI sanır; `-Raw | Set-Content` ile geri yazınca karakterler bozulur (`Tasarım` → `TasarÄ±m`). Okurken `[IO.File]::ReadAllText(yol, UTF8)`, yazarken UTF-8 kullan; ya da dosyayı Edit/Write araçlarıyla düzenle. 13 Ekim'de `karar_defteri.md` ve `adim3.ps1` bu yüzden bozuldu, düzeltildi (commit `077c9a3` bozuk sürümü içerir, sonraki commit onarılmış hâli).
- **Ondalık ayraç:** Türkçe yerel ayarda `21,2` yazılır, CSV bozulur. Sayıları `InvariantCulture` ile yaz.
- **Konsol çıktısı:** Dosyaya yönlendirilen çıktı UTF-8'dir; ekranda bozuk görünüyorsa `Get-Content -Encoding UTF8` ile oku.
- **Sunucuya ad ile bağlanmak** Docker'da ölçümü bozar (DNS artefaktı). Koşular IP ile yapılır (günlük 04, Bulgu 5).
- **Toplam "bağlandı" sayısına güvenme:** kesinti sırasındaki deneme sayısını da doğrula (günlük 04, Bulgu 1).
