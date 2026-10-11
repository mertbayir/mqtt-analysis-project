# 03 — Adım 2b: Üç bekleme kuralının ilk karşılaştırması (100 cihaz)

Tarih: 12 Ekim 2026
Durum: **Tamamlandı. Ön gözlem, istatistik değil** (her kural için tek koşu, tek tohum)
Rapor bölümü: Bulgular (ön), Tartışma (H1 sağlaması)

## Amaç

Üç yeniden bağlanma kuralının (A faktörü) aynı kesintide nasıl davrandığını 100 cihazla yan yana görmek. Düzeltilmiş `Device.cs` ile (bkz. 02, Ek 2).

## Kurulum

| Öğe | Değer |
|---|---|
| Cihaz | 100, tohum 1, süre 100 sn |
| Kesinti | Temiz: `docker stop`, ~21-23 sn kapalı (docker stop süresi koşuya göre değişiyor), `docker start` |
| Kurallar | `sabit` (1 sn), `ustel` (1→30 sn, rastgelesiz), `ustel-rastgele` (ilk U(0,5 sn), sonra U(0,5W, W)) |
| Betik | `project/scripts/adim2.ps1 -Politika <kural>` |
| Ham sonuçlar | `project/results/adim2_<kural>_n100_s1.{csv,konsol.txt,olaylar.txt}` |

Toparlanma süresi, `docker start` komutunun verildiği andan bağlı cihaz sayısının %50 / %95 / %100'e ulaştığı ilk saniyeye kadar (saniye çözünürlüğü, yaklaşık değer).

## Sonuçlar

| Kural | Kesintide tepe deneme/sn | Kesintide toplam deneme | Toparlanma %50 | %95 | %100 |
|---|---|---|---|---|---|
| sabit (1 sn) | **100** | 599 | 0,5 sn | 2,5 sn | 2,5 sn |
| üstel (rastgelesiz) | **99** | 300 | 3,1 sn | 4,1 sn | 4,1 sn |
| üstel + rastgele | **39** | 369 | 3 sn | 13 sn | 16 sn |

Deneme sayısı zaman serisi (kopmadan sonra, deneme yapılan saniyeler):

- **sabit:** t16:100, t17:64, t18:35, t21:100, t26:100, t31:100, t36:100 → her ~5 sn'de bir **100'lük dalga**
- **üstel:** t16:99, t18:14, t19:86, t26:9, t27:91 → seyrek ama yine **tek saniyede ~90-100'lük dalgalar**
- **üstel + rastgele:** t15-t38 arası 2 ile 39 arasında, **dalga yok, yayılmış**

## Yorum (ön)

1. **Rastgelesiz kurallarda cihazlar kilit adım hareket ediyor.** Sabit kuralda 100 cihaz her 5 sn'de bir aynı saniyede dener. Üstel kural dalgaları seyrekleştirir (toplam deneme 599 → 300) ama dalga boyunu küçültmez (tepe ~100).
2. **Rastgelelik tepeyi düşürüyor:** tepe 100 → 39. Bu, literatürdeki bilinen sonuçla uyumlu (H1, `14` Bölüm 3): rastgele bekleme tepe bağlantı denemesini azaltır. **Ortamın doğru çalıştığına dair ilk işaret.**
3. **Bedeli toparlanma süresi:** rastgeleli kuralda %100'e 16 sn'de ulaşıldı, sabit kuralda 2,5 sn. Cihazlar sunucu açıldığında hâlâ uzun bir bekleme ortasında olabiliyor (geri çekilme sunucunun ne zaman döneceğini bilmiyor). Bu, notlardaki "dengeleme" bulgusunun ilk küçük örneği.

## Sınırlar (raporda mutlaka yazılmalı)

- Her kural için **tek koşu, tek tohum.** Fark rastgele olabilir. Sayılar medyan/güven aralığı değil.
- **100 cihaz sunucuyu hiç zorlamıyor.** Başarısız denemelerin nedeni sunucunun kapalı olması, aşırı yük değil. Gerçek "izdiham" etkisi (sunucunun yetişememesi) bu ölçekte görünmez. Adım 3'te cihaz sayısı artırılacak.
- Kesintide cihazların denemeleri sunucu kapalıyken başarısız oluyor. "Toparlanma" burada sunucunun açılış anına bağlı. Rastgeleli kuralın 16 sn'si kısmen sunucunun açık olduğu süreyi cihazların fark edememesinden geliyor.
- Kapalı kalma süresi koşular arasında 20,9–23 sn (docker stop'un sürmesi). Koşular arası küçük fark yaratır, sabit tutulması gerekiyor (sonraki adımda sunucuyu durdurma anı ve süresi daha kontrollü olmalı).
- Çözünürlük 1 sn. Toparlanma süreleri ±1 sn.
- Toparlanma ölçütü yalnızca "bağlı cihaz sayısı". Cihazların bağlandıktan sonra çalıştığı kanıtlanmadı (henüz eşitleme/boşaltma yok).

## Sonraki adım

Adım 3: cihaz sayısını 1.000 → 5.000'e çıkarmak ve sunucunun gerçekten zorlanıp zorlanmadığına (fırtına çıkıyor mu) bakmak. Öncesinde `.wslconfig` ile Docker kaynak sınırı açıkça yazılacak, simülatör Docker ağının içine alınacak.
