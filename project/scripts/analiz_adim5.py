"""Adım 5 (bağlantı sonrası yük, canlı alarm gecikmesi) analizi.

Kullanım:  python scripts/analiz_adim5.py <sürüm_etiketi> [--kosu <ad>]      örn: python scripts/analiz_adim5.py v6
Çıktı:     results/ozet_adim5_<sürüm>_kosular.csv, ..._hucreler.csv, ekrana tablo

Zaman ekseni: simülatör başlangıcından beri geçen saniye, TEK YÖNLÜ saatle (CLOCK_MONOTONIC):
  t = collector.mono_s - girdi.baslangic_mono_ms/1000   (duvar saati WSL2'de sıçrar, günlük 06 Bulgu 1)
  kopma = sunucu durdurulduğunda bağlı sayısının ilk kez 0 olduğu saniye (simülatör CSV'si, %90'a ulaştıktan sonra)
  basla = kopma + kapali_sn (sunucunun yeniden başlatıldığı an, kayıt defterinden)

Ölçütler (hepsi koşu başına):
  taban_p99_ms        : kesinti ÖNCESİ canlı alarm gecikmesinin saniyelik p99 değerlerinin medyanı
  tepe_p99_ms         : sunucu başlatıldıktan sonraki saniyelik canlı alarm p99'un en büyüğü
  eşik_üstü_sn        : sunucu başlatıldıktan sonra canlı alarm saniyelik p99'un 100 ms'yi aştığı saniye sayısı
  vital_tepe_p99_ms   : aynısı canlı vital için
  bosaltma_bitis_sn   : basla'dan son biriken mesajın toplayıcıya ulaşmasına kadar (sn)
  bosaltma_tepe_mesaj : saniyede toplayıcıya ulaşan en çok biriken mesaj
  sync_p50/p99_ms, sync_hata : eşitleme isteği→cevap süresi (cihaz tarafı)
  flush_bitis_p50/p95/max_sn : cihazların boşaltmayı bitirdiği an (basla'dan itibaren)
  kayip               : üretilen - (alınan canlı + alınan biriken) - atılan; 0 olmalı (B0'da atılan sayılır)
  saat_sicramasi      : duvar saati sıçrama olayı sayısı (gecikmeyi etkilemez, bilgi)
  bir_alarm_yas_*     : KESİNTİDE BİRİKEN alarmın yaşı (toplayıcıya ulaştığı an - planlanan üretim anı, sn): p50, p95 (adet
                        ağırlıklı, saniye başına yüzdeliklerden yaklaşık), en büyük. Günlük 13.
  alarm_flush_bitis_max_sn : B3'te cihazların birikmiş alarmları bitirdiği son an (basla'dan, sn); diğer politikalarda boş
"""
import json
import math
import sys
from pathlib import Path

import pandas as pd

SURUM = sys.argv[1] if len(sys.argv) > 1 else "v6"
TEK = sys.argv[sys.argv.index("--kosu") + 1] if "--kosu" in sys.argv else None
SONUC = Path(__file__).resolve().parent.parent / "results"
ESIK_MS = 100

kayit = pd.read_csv(SONUC / "deney_kayitlari.csv", encoding="utf-8-sig")
kayit = kayit[kayit["kosu"].str.startswith("adim5_")]
kayit = kayit[kayit["kosu"] == TEK] if TEK else kayit[kayit["kosu"].str.endswith(f"_{SURUM}")]


def cpu_tepe(yol, bas, bit, t0_utc=None):
    return float("nan")


def analiz(k):
    ad = k["kosu"]
    n = int(k["cihaz"])
    g = json.load(open(SONUC / f"{ad}.girdi.json", encoding="utf-8"))
    t0 = g["baslangic_mono_ms"] / 1000.0
    kapali = float(k["kapali_sn"])
    sim = pd.read_csv(SONUC / f"{ad}.csv")
    ulasti = sim["bagli"].cummax() >= math.ceil(0.9 * n)
    kopma = int(sim.loc[ulasti & (sim["bagli"] == 0), "saniye"].iloc[0])
    basla = kopma + kapali
    kesinti_turu = str(k.get("kesinti_turu", "temiz"))
    sessiz_yayilim = float("nan")
    if kesinti_turu == "sessiz":
        # Sessiz kesintide bağlı sayısı sıfıra 10-15 sn'de iner; kopma ve başlama anları olay zamanlarından alınır
        ol = open(SONUC / f"{ad}.olaylar.txt", encoding="utf-8-sig").read().splitlines()

        def _t(satir):
            h, m, sn = satir.split()[0].split(":")
            return int(h) * 3600 + int(m) * 60 + float(sn)
        t_sim = _t(next(x for x in ol if "simulator basladi" in x))
        t_kes = _t(next(x for x in ol if "docker stop" in x and "bitti" not in x))
        t_ac = _t(next(x for x in ol if "docker start" in x))
        kopma = int(round(t_kes - t_sim))
        basla = t_ac - t_sim
        after = sim[sim["saniye"] >= kopma]
        ilk0 = after.loc[after["bagli"] == 0, "saniye"]
        sessiz_yayilim = float(ilk0.iloc[0] - kopma) if len(ilk0) else float("nan")

    col = pd.read_csv(SONUC / f"{ad}.collector.csv")
    col["t"] = col["mono_s"] - t0

    def seri(sinif):
        return col[col["sinif"] == sinif].set_index("t")

    alarm, vital = seri("canli_alarm"), seri("canli_vital")
    biriken = pd.concat([seri("biriken_alarm"), seri("biriken_vital")])

    # taban: bağlantıların oturduğu ve kesintiden önceki dönem (başlangıç yayılımı sonrası)
    yay = float(g.get("ilk_baglanma_yayilimi_sn", 0))
    taban = alarm[(alarm.index >= yay + 5) & (alarm.index < kopma - 1)]["p99_ms"]
    sonra_a = alarm[alarm.index >= basla]["p99_ms"]
    sonra_v = vital[vital.index >= basla]["p99_ms"]
    bsonra = biriken[biriken.index >= basla]

    olay = pd.read_csv(SONUC / f"{ad}.olay.csv")
    sy = olay[olay["tur"] == "sync_ok"]
    fl = olay[olay["tur"].isin(["flush_bitti", "atildi"])]
    fl_rel = fl["t_sn"] - basla
    toplam = json.load(open(SONUC / f"{ad}.toplam.json", encoding="utf-8"))
    ctop = json.load(open(SONUC / f"{ad}.collector.csv.toplam.json", encoding="utf-8"))
    alinan = sum(ctop["benzersiz_alinan"].values())
    uretilen = toplam["uretilen_vital"] + toplam["uretilen_alarm"]
    atilan = toplam["atilan_biriken"]

    ba = seri("biriken_alarm")

    def yas(kol, q):
        if ba.empty:
            return float("nan")
        d = ba.sort_values(kol)
        c = d["adet"].cumsum()
        return float(d.loc[c >= q * c.iloc[-1], kol].iloc[0]) / 1000.0
    af = olay[olay["tur"] == "alarm_flush_bitti"]

    bro = SONUC / f"{ad}.broker.csv"
    cpu = float("nan")
    if bro.exists():
        cpu = pd.read_csv(bro)["cpu_yuzde"].max()

    return {
        "kosu": ad, "A": k["politika"].split("|")[0], "B": k["politika"].split("|")[1], "cihaz": n, "tohum": int(k["tohum"]),
        "kapali_sn": kapali, "kesinti": kesinti_turu, "dusus_suresi_sn": sessiz_yayilim,
        "taban_p99_ms": float(taban.median()) if len(taban) else float("nan"),
        "tepe_p99_ms": float(sonra_a.max()) if len(sonra_a) else float("nan"),
        "esik_ustu_sn": int((sonra_a > ESIK_MS).sum()),
        "vital_tepe_p99_ms": float(sonra_v.max()) if len(sonra_v) else float("nan"),
        "bosaltma_bitis_sn": round(float(bsonra.index.max() - basla), 1) if len(bsonra) else float("nan"),
        "bosaltma_tepe_mesaj": int(bsonra.groupby(level=0)["adet"].sum().max()) if len(bsonra) else 0,
        "sync_p50_ms": float(sy["sure_ms"].median()) if len(sy) else float("nan"),
        "sync_p99_ms": float(sy["sure_ms"].quantile(0.99)) if len(sy) else float("nan"),
        "sync_hata": toplam["sync_hata"],
        "flush_bitis_p50_sn": round(float(fl_rel.median()), 1) if len(fl) else float("nan"),
        "flush_bitis_max_sn": round(float(fl_rel.max()), 1) if len(fl) else float("nan"),
        "uretilen": uretilen, "alinan": alinan, "atilan": atilan,
        "kayip": uretilen - alinan - atilan - toplam["gonderim_hatasi"] * 0,
        "tekrar": ctop["tekrar_alinan"], "negatif_gecikme": ctop["negatif_gecikmeli_mesaj_20ms_alti"],
        "saat_sicramasi": int((olay["tur"] == "saat_sicramasi").sum()),
        "srv_cpu_tepe": cpu,
        "bir_alarm_adet": int(ba["adet"].sum()) if len(ba) else 0,
        "bir_alarm_yas_p50_s": round(yas("p50_ms", 0.5), 1),
        "bir_alarm_yas_p95_s": round(yas("p95_ms", 0.95), 1),
        "bir_alarm_yas_max_s": round(float(ba["max_ms"].max()) / 1000.0, 1) if len(ba) else float("nan"),
        "alarm_flush_bitis_max_sn": round(float((af["t_sn"] - basla).max()), 1) if len(af) else float("nan"),
    }


satirlar = [analiz(k) for _, k in kayit.iterrows()]
if not satirlar:
    sys.exit("analiz edilecek koşu bulunamadı")
df = pd.DataFrame(satirlar).sort_values(["cihaz", "A", "B", "tohum"])
etiket = TEK or SURUM
df.to_csv(SONUC / f"ozet_adim5_{etiket}_kosular.csv", index=False)

pd.set_option("display.width", 250)
pd.set_option("display.max_columns", 40)
print(f"Koşu sayısı: {len(df)}\n")
print(df.drop(columns=["kosu"]).to_string(index=False))

if len(df) > 1 and TEK is None:
    olcutler = ["taban_p99_ms", "tepe_p99_ms", "esik_ustu_sn", "vital_tepe_p99_ms", "bosaltma_bitis_sn",
                "bosaltma_tepe_mesaj", "sync_p99_ms", "flush_bitis_max_sn", "srv_cpu_tepe",
                "bir_alarm_yas_p50_s", "bir_alarm_yas_p95_s", "bir_alarm_yas_max_s"]
    hucre = []
    for (n, a, b), g in df.groupby(["cihaz", "A", "B"]):
        s = {"cihaz": n, "A": a, "B": b, "tekrar": len(g)}
        for o in olcutler:
            s[f"{o}_med"], s[f"{o}_min"], s[f"{o}_max"] = g[o].median(), g[o].min(), g[o].max()
        hucre.append(s)
    h = pd.DataFrame(hucre)
    h.to_csv(SONUC / f"ozet_adim5_{etiket}_hucreler.csv", index=False)
    print("\nHÜCRE BAŞINA: medyan [en küçük - en büyük]")
    for _, r in h.iterrows():
        def f(o):
            return f"{r[o+'_med']:g} [{r[o+'_min']:g}-{r[o+'_max']:g}]"
        print(f"N={int(r['cihaz']):>5} A={r['A']:<14} B={r['B']} n={int(r['tekrar'])} | tepe_p99 {f('tepe_p99_ms'):<22} | "
              f"eşik_üstü_sn {f('esik_ustu_sn'):<12} | boşaltma_bitiş {f('bosaltma_bitis_sn'):<16} | sync_p99 {f('sync_p99_ms'):<20} | srv_cpu {f('srv_cpu_tepe')}")
