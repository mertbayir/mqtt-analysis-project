"""Sonuç gezgini (web/sonuc_gezgini.html) için veriyi results/ altındaki dosyalardan üretir.

Kullanım:  python scripts/web_uret.py
Girdi:     results/deney_kayitlari.csv, her koşunun <ad>.csv (saniyelik bağlı sayısı), <ad>.girdi.json,
           <ad>.collector.csv (canlı alarm gecikmesi), results/ozet_adim4*_kosular.csv, ozet_adim5_*_kosular.csv
Çıktı:     web/veri.js (yalnızca veri), web/sonuc_gezgini.html (şablon + veri, tek dosya, internetsiz çalışır)

Zaman ekseni: 0 = sunucunun yeniden başlatıldığı an (kopma + kapalı süre). Yeni koşular (EMQX) bitince betik
yeniden çalıştırılır; eksik dosyalı koşular atlanır (uyarı yazar).
"""
import json
import math
from pathlib import Path

import pandas as pd

PROJE = Path(__file__).resolve().parent.parent
SONUC = PROJE / "results"
WEB = PROJE / "web"
WEB.mkdir(exist_ok=True)

# Gösterilecek sürümler: (sürüm etiketi, adım ailesi, özet dosyası)
SURUMLER = [
    ("v5", "adim3", "ozet_adim4_kosular.csv"),
    ("t1", "adim3", "ozet_adim4_t1_kosular.csv"),
    ("v6", "adim5", "ozet_adim5_v6_kosular.csv"),
    ("v7", "adim5", "ozet_adim5_v7_kosular.csv"),
    ("v8", "adim5", "ozet_adim5_v8_kosular.csv"),
    ("tls6", "adim5", "ozet_adim5_tls6_kosular.csv"),
    ("e1", "adim5", "ozet_adim5_e1_kosular.csv"),
    ("e2", "adim5", "ozet_adim5_e2_kosular.csv"),
    ("p1", "adim5", "ozet_adim5_p1_kosular.csv"),
    ("p2", "adim5", "ozet_adim5_p2_kosular.csv"),
]

kayit = pd.read_csv(SONUC / "deney_kayitlari.csv", encoding="utf-8-sig")
KAPSAM = (-115, 175)   # saniye, sunucu açılışına göre


def sayi(x, nd=1):
    if x is None or (isinstance(x, float) and math.isnan(x)):
        return None
    return round(float(x), nd)


runlar = []
for surum, aile, ozet_dosyasi in SURUMLER:
    ozet_yolu = SONUC / ozet_dosyasi
    ozet = pd.read_csv(ozet_yolu).set_index("kosu") if ozet_yolu.exists() else None
    k_surum = kayit[kayit["kosu"].str.startswith(aile + "_") & kayit["kosu"].str.endswith("_" + surum)]
    if k_surum.empty:
        continue
    for _, k in k_surum.iterrows():
        ad = k["kosu"]
        try:
            n = int(k["cihaz"])
            sim = pd.read_csv(SONUC / f"{ad}.csv")
            g = json.load(open(SONUC / f"{ad}.girdi.json", encoding="utf-8"))
            ulasti = sim["bagli"].cummax() >= math.ceil(0.9 * n)
            kopma = int(sim.loc[ulasti & (sim["bagli"] == 0), "saniye"].iloc[0])
            basla = kopma + float(k["kapali_sn"])
            ofs = round(basla)
            a, b = k["politika"].split("|")[0], k["politika"].split("|")[1] if "|" in k["politika"] else ""
            run = {
                "id": ad, "aile": aile, "surum": surum,
                "sunucu": "emqx" if str(k["sunucu"]).startswith("emqx") else "mosquitto",
                "tls": bool(g.get("tls", surum in ("t1", "tls6"))),
                "cpu": int(str(k["sunucu_kaynak"]).split(" CPU")[0].strip().split()[-1]) if "CPU" in str(k["sunucu_kaynak"]) else 2,
                "A": a, "B": b, "N": n, "tohum": int(k["tohum"]),
                "kapali": round(float(k["kapali_sn"])),
            }
            t_bas = max(KAPSAM[0], 1 - ofs)
            t_son = min(KAPSAM[1], int(sim["saniye"].max()) - ofs)
            bagli, deneme = [], []
            bm = sim.set_index("saniye")
            for t in range(t_bas, t_son + 1):
                s = t + ofs
                bagli.append(int(bm["bagli"].get(s, 0)))
                deneme.append(int(bm["deneme"].get(s, 0)))
            run.update({"t0": t_bas, "bagli": bagli, "deneme": deneme})

            if aile == "adim5":
                col = pd.read_csv(SONUC / f"{ad}.collector.csv")
                col = col[col["sinif"] == "canli_alarm"].copy()
                col["t"] = (col["mono_s"] - g["baslangic_mono_ms"] / 1000.0 - basla).apply(math.floor)
                al = col.groupby("t")["p99_ms"].max()
                run["alarm"] = [int(al[t]) if t in al.index else None for t in range(t_bas, t_son + 1)]
            if ozet is not None and ad in ozet.index:
                o = ozet.loc[ad]
                if aile == "adim5":
                    run["ozet"] = {
                        "taban": sayi(o["taban_p99_ms"], 0), "tepe": sayi(o["tepe_p99_ms"], 0),
                        "esik": int(o["esik_ustu_sn"]), "bosaltma": sayi(o["bosaltma_bitis_sn"], 1),
                        "sync_p99": sayi(o["sync_p99_ms"], 0), "cpu": sayi(o["srv_cpu_tepe"], 0),
                        "kayip": int(o["kayip"]), "tekrar": int(o["tekrar"]),
                        "yas_p50": sayi(o.get("bir_alarm_yas_p50_s"), 1), "yas_max": sayi(o.get("bir_alarm_yas_max_s"), 1),
                    }
                else:
                    run["ozet"] = {
                        "tepe_kesinti": int(o["tepe_kesinti"]), "ttr50": sayi(o["TTR50"], 1), "ttr100": sayi(o["TTR100"], 1),
                        "hata_sonra": int(o["hata_sonra"]), "cpu": sayi(o["srv_cpu_tepe"], 0),
                    }
            runlar.append(run)
        except Exception as e:  # eksik dosya / bozuk koşu
            print(f"UYARI: {ad} atlandı ({type(e).__name__}: {e})")

# Hipotez karnesi (sonuçlar günlüklerden; yeni sonuç geldikçe güncellenir)
D, C, K, B_, S = "destek", "curudu", "kismen", "bekliyor", "sinanamadi"
hipotezler = [
    ("Adım 4", "H1", "Rastgeleli kural kesintideki tepe denemeyi düşürür", D, "tepe N'nin ≈ %25'ine indi"),
    ("Adım 4", "H2", "Rastgeleli kural toparlanma sonunu uzatır", D, "TTR %100: 34-35 sn (üstel 25 sn)"),
    ("Adım 4", "H3", "TLS'siz sunucu bağlanma aşamasında zorlanmaz", D, "CPU en çok 30 / 200"),
    ("Adım 4", "H4", "N = 2.000 ile 5.000 arasında devrilme yok", D, "TTR farkı ≤ 1 sn"),
    ("Adım 5", "H5", "Üstel + B1: alarm gecikmesi tabanın ≥ 10 katı (ikinci izdiham)", D, "9.158 ms (≈ 300 kat)"),
    ("Adım 5", "H6", "B2, B1'e göre tepeyi ≥ %50 düşürür", K, "üstelde %98, rastgelelide düşürmedi"),
    ("Adım 5", "H7", "Rastgeleli A + B1, üstel A + B1'den düşük tepe", D, "9.158 → 51 ms"),
    ("Adım 5", "H8", "B0 taban düzeyinde kalır", D, "47-60 ms (veriyi atarak)"),
    ("Adım 5", "H9", "Kayıp 0 ve tekrar 0", K, "2 koşuda 1 mesaj kayıp"),
    ("Adım 5", "H10", "B1'de sunucu CPU ≥ %80 (tek çekirdek)", D, "%85-98"),
    ("Adım 5b", "H11", "Rastgeleli + B1, N = 10.000'de doyar", D, "6.157 ms"),
    ("Adım 5b", "H12", "B2 rastgelelide B1'den iyi (N = 10.000)", C, "7.324 vs 6.157 ms"),
    ("Adım 5b", "H13", "B2 ile tepe < 500 ms (N = 10.000)", C, "7.324 ve 9.259 ms"),
    ("Adım 5b", "H14", "Üstel + B1, rastgeleli + B1'den kötü", D, "19.741 vs 6.157 ms"),
    ("Adım 5b", "H15", "Tekrar 0, kayıp ≤ 1", K, "91 ve 132 mesaj kayıp (2 koşu)"),
    ("Adım 5c", "H16", "B0, N = 10.000'de de tabana yakın", D, "90 ms"),
    ("Adım 5c", "H17", "B2 60 sn / 1 sn: tepe < 500 ms", D, "237 ms"),
    ("Adım 5c", "H18", "B2 90 sn / 500 ms: tepe < 500 ms", D, "169 ms"),
    ("Adım 5c", "H19", "60 sn / 1 sn: boşaltma ≥ 100 sn, kayıp ≤ 1", D, "138 sn, kayıp 0"),
    ("Adım 5c", "H20", "Tekrar 0", D, "9 koşuda 0"),
    ("Kayıp", "HK3", "Kayıp tohuma bağlı", C, "tohum 1 ve 3 tekrar koşulunca kayıp yok"),
    ("Kayıp", "HK1-2", "Kayıp, durdurma ya da abone penceresinde", S, "12 ham kayıtlı koşuda kayıp yok"),
    ("Adım 6 (TLS)", "HT1", "TLS'te üstel TTR ≥ 2 × TLS'siz", D, "62,9 vs 25,1 sn"),
    ("Adım 6 (TLS)", "HT2", "TLS'te üstelde başarısız bağlantı > 0", D, "1.119-1.919"),
    ("Adım 6 (TLS)", "HT3", "TLS'te rastgeleli kural TTR'yi kısaltır", D, "28,9 vs 62,9 sn (yön tersine döndü)"),
    ("Adım 6 (TLS)", "HT4", "TLS'te sunucu CPU ≥ 3 × TLS'siz", D, "3,5-5,2 kat"),
    ("Adım 6 (TLS)", "HT5", "Devrilme noktası 2.000 ile 5.000 arasında", C, "etki 2.000'de de var"),
    ("Adım 6 (TLS)", "HT6", "Sabit kural TLS'te ≥ 2 × kötü", C, "9,9 vs 7,1 sn"),
    ("Adım 6b", "HF1", "TLS'te B0 üstelde tabanın ≥ 5 katı", D, "613 ms (14-22 kat)"),
    ("Adım 6b", "HF2", "Üstel + B1, TLS'te TLS'siz değerden yüksek", C, "8.234 vs 9.158 ms"),
    ("Adım 6b", "HF3", "TLS'te rastgeleli + B1 tabanın ≥ 10 katı", D, "1.234 ms"),
    ("Adım 6b", "HF4", "TLS'te B2, B1'e göre ≥ %50 düşürür", D, "%89"),
    ("Adım 6b", "HF5", "Tekrar 0, kayıp ≤ 1", D, "18 koşuda 0"),
    ("Adım 7 (EMQX)", "HE1", "EMQX 2 CPU, üstel + B1: tepe ≥ 10 × taban", D, "16.188 ms (Mosquitto 9.158)"),
    ("Adım 7 (EMQX)", "HE2", "EMQX'te B2, B1'e göre ≥ %50 düşürür", K, "üstelde %99; rastgelelide B2 daha yüksek (163 vs 93 ms)"),
    ("Adım 7 (EMQX)", "HE3", "EMQX'te rastgeleli + B1: tepe ≥ 5 × taban", C, "93 ms (≈ 3 × taban)"),
    ("Adım 7 (EMQX)", "HE4", "EMQX'te B0 tepesi < 100 ms", C, "üstelde 112 ms"),
    ("Adım 7 (EMQX)", "HE5", "EMQX 4 CPU, üstel + B1: tepe 2 CPU'nun ≥ %50 altında", C, "11.527 vs 16.188 ms (%29); CPU 389/400"),
    ("Adım 7 (EMQX)", "HE6", "Tekrar 0, kayıp ≤ 1", K, "tekrar 0; kayıp 17/30 koşuda > 1 (en çok 69)"),
]
hipotezler += [
    ("Adım 8 (B3)", "HP1", "B3'te biriken alarmın yaş medyanı B2'den ≥ %25 düşük", K, "N = 10.000: %60; TLS rastgeleli %34; TLS üstel %24"),
    ("Adım 8 (B3)", "HP2", "B3'te biriken alarmın en büyük yaşı B2'den ≥ %25 düşük", K, "N = 10.000: %48; TLS: %24 ve %18"),
    ("Adım 8 (B3)", "HP3", "B3'te canlı alarm tepesi B2'nin ≤ 2 katı", K, "üç hücrede evet; TLS üstelde 2,3 kat"),
    ("Adım 8 (B3)", "HP4", "B3'te boşaltma bitişi B2'den ≤ %10 farklı", D, "%2-5"),
    ("Adım 8 (B3)", "HP5", "Tekrar 0, kayıp ≤ 1", K, "21 koşuda 1 koşu 42 kayıp"),
]
hip = [{"adim": a, "id": i, "metin": m, "durum": d, "kanit": k} for a, i, m, d, k in hipotezler]

veri = {"runlar": runlar, "hipotezler": hip, "kapsam": KAPSAM}
js = json.dumps(veri, ensure_ascii=False, separators=(",", ":"))
(WEB / "veri.js").write_text("window.VERI = " + js + ";\n", encoding="utf-8")
sablon = WEB / "sablon.html"
if sablon.exists():
    import base64
    mimari = PROJE.parent / "makale" / "sekiller" / "sekil_mimari.png"
    b64 = ""
    if mimari.exists():
        try:   # web için küçült (tek dosya hafif kalsın)
            from PIL import Image
            import io
            im = Image.open(mimari).convert("RGB")
            im = im.resize((1100, int(im.height * 1100 / im.width)))
            tampon = io.BytesIO(); im.save(tampon, format="PNG", optimize=True)
            b64 = base64.b64encode(tampon.getvalue()).decode("ascii")
        except Exception:
            b64 = base64.b64encode(mimari.read_bytes()).decode("ascii")
    html = sablon.read_text(encoding="utf-8").replace("/*__VERI__*/", "window.VERI = " + js + ";").replace("/*__MIMARI__*/", b64)
    model_js = (WEB / "model.js").read_text(encoding="utf-8") if (WEB / "model.js").exists() else ""
    par = WEB / "model_param.json"
    art = WEB / "model_artik.json"
    param_js = ("window.MODEL_PARAM = " + par.read_text(encoding="utf-8") + ";\nwindow.MODEL_ARTIK = " + (art.read_text(encoding="utf-8") if art.exists() else "[]") + ";") if par.exists() else ""
    html = html.replace("/*__MODEL__*/", model_js).replace("/*__PARAM__*/", param_js)
    (WEB / "sonuc_gezgini.html").write_text(html, encoding="utf-8")
    print("yazıldı: web/sonuc_gezgini.html")
print(f"koşu: {len(runlar)}  veri boyutu: {len(js) / 1024:.0f} KB")
