"""Tekrarlı koşuların özeti: koşu başına ölçütler + hücre (kural x N) başına medyan ve aralık.

Kullanım:  python scripts/tekrar_ozet.py [sürüm_etiketi]      (varsayılan: v5)
Çıktı:     results/ozet_adim4_kosular.csv, ozet_adim4_hucreler.csv (v5); diğer sürümler: ozet_adim4_<sürüm>_*.csv; ekrana tablo

Ölçüt tanımları scripts/ozet.ps1 ile aynıdır:
  kopma       : bağlı sayısı en az %90'a ulaştıktan sonra ilk kez 0 olan saniye
  basla       : kopma + kapali_sn (sunucunun yeniden başlatıldığı an)
  tepe_kesinti: [kopma, basla] aralığında bir saniyede en çok bağlantı denemesi
  tepe_sonra  : basla'dan sonra bir saniyede en çok deneme
  hata_sonra  : basla'dan sonraki toplam başarısız bağlantı (sunucu AÇIKKEN)
  TTR50/95/100: basla'dan sonra bağlı sayısı %50/%95/%100'e ilk ulaştığı an - basla (sn)
n = 3 ile güven aralığı verilmez: yalnızca medyan, en küçük, en büyük.
"""
import math
import sys
from pathlib import Path

import pandas as pd

SURUM = sys.argv[1] if len(sys.argv) > 1 else "v5"
SONUC = Path(__file__).resolve().parent.parent / "results"
kayit = pd.read_csv(SONUC / "deney_kayitlari.csv", encoding="utf-8-sig")
kayit = kayit[kayit["kosu"].str.endswith(f"_{SURUM}")]


def cpu_tepe(yol):
    if not yol.exists():
        return float("nan")
    return pd.read_csv(yol)["cpu_yuzde"].max()


satirlar = []
for _, k in kayit.iterrows():
    df = pd.read_csv(SONUC / f"{k['kosu']}.csv")
    n = int(k["cihaz"])
    kapali = float(k["kapali_sn"])
    ulasti = df["bagli"].cummax() >= math.ceil(0.9 * n)
    kopma = int(df.loc[ulasti & (df["bagli"] == 0), "saniye"].iloc[0])
    basla = kopma + kapali
    kesinti = df[(df["saniye"] >= kopma) & (df["saniye"] <= basla)]
    sonra = df[df["saniye"] > basla]
    ttr = {}
    for oran in (50, 95, 100):
        esik = math.ceil(n * oran / 100)
        x = df[(df["saniye"] >= basla) & (df["bagli"] >= esik)]
        ttr[oran] = round(float(x["saniye"].iloc[0]) - basla, 1) if len(x) else float("nan")
    satirlar.append({
        "kosu": k["kosu"], "kural": k["politika"], "cihaz": n, "tohum": int(k["tohum"]),
        "kapali_sn": kapali, "tepe_kesinti": int(kesinti["deneme"].max()),
        "toplam_deneme": int(kesinti["deneme"].sum()), "tepe_sonra": int(sonra["deneme"].max()),
        "hata_sonra": int(sonra["hata"].sum()), "TTR50": ttr[50], "TTR95": ttr[95], "TTR100": ttr[100],
        "srv_cpu_tepe": cpu_tepe(SONUC / f"{k['kosu']}.broker.csv"),
        "sim_cpu_tepe": cpu_tepe(SONUC / f"{k['kosu']}.sim.csv"),
    })

kosular = pd.DataFrame(satirlar).sort_values(["cihaz", "kural", "tohum"])
_ek = "" if SURUM == "v5" else f"_{SURUM}"   # v5 dosya adları değişmesin; diğer sürümler kendi dosyasına yazar
kosular.to_csv(SONUC / f"ozet_adim4{_ek}_kosular.csv", index=False)

olcutler = ["tepe_kesinti", "toplam_deneme", "tepe_sonra", "hata_sonra", "TTR50", "TTR95", "TTR100",
            "srv_cpu_tepe", "sim_cpu_tepe"]
hucre_satirlari = []
for (cihaz, kural), g in kosular.groupby(["cihaz", "kural"]):
    s = {"cihaz": cihaz, "kural": kural, "tekrar": len(g)}
    for o in olcutler:
        s[f"{o}_med"] = g[o].median()
        s[f"{o}_min"] = g[o].min()
        s[f"{o}_max"] = g[o].max()
    hucre_satirlari.append(s)
hucreler = pd.DataFrame(hucre_satirlari)
hucreler.to_csv(SONUC / f"ozet_adim4{_ek}_hucreler.csv", index=False)

pd.set_option("display.width", 220)
pd.set_option("display.max_columns", 30)
print(f"Koşu sayısı: {len(kosular)}  (sürüm {SURUM})\n")
print("KOŞU BAŞINA")
print(kosular.drop(columns=["kosu"]).to_string(index=False))
print("\nHÜCRE BAŞINA: medyan [en küçük - en büyük]")
for _, h in hucreler.iterrows():
    def f(o):
        return f"{h[o+'_med']:g} [{h[o+'_min']:g}-{h[o+'_max']:g}]"
    print(f"N={int(h['cihaz']):>5} {h['kural']:<15} n={int(h['tekrar'])} | tepe_kes {f('tepe_kesinti'):<22} | "
          f"TTR100 {f('TTR100'):<18} | TTR50 {f('TTR50'):<16} | hata_sonra {f('hata_sonra'):<24} | "
          f"srv_cpu {f('srv_cpu_tepe'):<16}")
