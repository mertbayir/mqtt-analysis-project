"""Kayıp analizi: toplayıcıya ulaşmayan mesajları bulur ve ne zaman gönderildiklerini tahmin eder.

Kullanım:  python scripts/kayip_analizi.py <sürüm_etiketi>            örn: python scripts/kayip_analizi.py k1
Girdi:     results/<koşu>.collector.ham.csv (toplayıcı ham kaydı, adim5.ps1 -HamKayit), .collector.csv.toplam.json,
           .girdi.json, .toplam.json
Çıktı:     results/kayip_<etiket>.csv  (kayıp mesaj başına bir satır) ve ekrana özet

Yöntem: her cihazın mesaj numarası (seq) 1'den başlayıp birer artar. Toplayıcının aldığı numaralardaki boşluklar kayıp
mesajlardır. Son numaradan sonrası boşlukla görülemez; bu "kuyruk kaybı" üretilen - alınan farkından hesaplanır.
Zaman: tek yönlü saat (mono), sunucu durdurmadan beri geçen saniye cinsinden (t_dur = toplayıcının ilk kopma anı).
"""
import json
import re
import sys
from pathlib import Path

import pandas as pd

ETIKET = sys.argv[1]
SONUC = Path(__file__).resolve().parent.parent / "results"
kayit = pd.read_csv(SONUC / "deney_kayitlari.csv", encoding="utf-8-sig")
kayit = kayit[kayit["kosu"].str.startswith("adim5_") & kayit["kosu"].str.endswith(f"_{ETIKET}")]

satirlar, ozetler = [], []
for _, k in kayit.iterrows():
    ad = k["kosu"]
    ham_yolu = SONUC / f"{ad}.collector.ham.csv"
    if not ham_yolu.exists():
        continue
    ham = pd.read_csv(ham_yolu)
    ctop = json.load(open(SONUC / f"{ad}.collector.csv.toplam.json", encoding="utf-8"))
    stop = json.load(open(SONUC / f"{ad}.toplam.json", encoding="utf-8"))
    uretilen = stop["uretilen_vital"] + stop["uretilen_alarm"]
    alinan = ham.drop_duplicates(["cihaz", "seq"]).shape[0]
    fark = uretilen - alinan

    olaylar = []
    for o in ctop["baglanti_olaylari"]:
        m = re.search(r"(koptu|baglandi) mono_ms=(\d+)", o)
        if m:
            olaylar.append((m.group(1), int(m.group(2))))
    koptu = [t for tur, t in olaylar if tur == "koptu"]
    baglandi = [t for tur, t in olaylar if tur == "baglandi"]
    t_dur = koptu[0] if koptu else None          # sunucu durduruldu (toplayıcı kopmayı gördü)
    t_abone = baglandi[-1] if len(baglandi) > 1 else None   # toplayıcı sunucu açılınca yeniden abone oldu

    bosluk = 0
    for cihaz, g in ham.groupby("cihaz"):
        g = g.drop_duplicates("seq").sort_values("seq").reset_index(drop=True)
        seq = g["seq"].to_numpy()
        for i in range(1, len(seq)):
            if seq[i] - seq[i - 1] > 1:
                onceki, sonraki = g.iloc[i - 1], g.iloc[i]
                for eksik in range(int(seq[i - 1]) + 1, int(seq[i])):
                    bosluk += 1
                    satirlar.append({
                        "kosu": ad, "cihaz": int(cihaz), "seq": eksik,
                        "onceki_tur": onceki["tur"], "onceki_biriken": int(onceki["biriken"]),
                        "onceki_plan_t_dur_sn": round((onceki["planlanan_ms"] - t_dur) / 1000, 2) if t_dur else None,
                        "onceki_alinma_t_dur_sn": round((onceki["alinma_ms"] - t_dur) / 1000, 2) if t_dur else None,
                        "sonraki_tur": sonraki["tur"], "sonraki_biriken": int(sonraki["biriken"]),
                        "sonraki_plan_t_dur_sn": round((sonraki["planlanan_ms"] - t_dur) / 1000, 2) if t_dur else None,
                        "sonraki_alinma_t_dur_sn": round((sonraki["alinma_ms"] - t_dur) / 1000, 2) if t_dur else None,
                        "abone_t_dur_sn": round((t_abone - t_dur) / 1000, 2) if t_dur and t_abone else None,
                    })
    ozetler.append({"kosu": ad, "uretilen": uretilen, "alinan": alinan, "kayip": fark, "bosluk_olarak_gorulen": bosluk,
                    "kuyruk_kaybi": fark - bosluk,
                    "abone_gecikmesi_sn": round((t_abone - t_dur) / 1000 - float(k["kapali_sn"]), 2) if t_dur and t_abone else None})

if not ozetler:
    sys.exit("ham kayıtlı koşu bulunamadı")
pd.set_option("display.width", 250)
pd.set_option("display.max_columns", 30)
oz = pd.DataFrame(ozetler)
print(oz.to_string(index=False))
if satirlar:
    df = pd.DataFrame(satirlar)
    df.to_csv(SONUC / f"kayip_{ETIKET}.csv", index=False)
    print("\nKAYIP MESAJLAR (komşu mesajların zamanı: sunucu durdurulmasından beri sn):")
    print(df.to_string(index=False))
oz.to_csv(SONUC / f"kayip_{ETIKET}_ozet.csv", index=False)
