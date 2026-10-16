"""Makale şekillerini results/ altındaki CSV'lerden üretir. Şekiller elle çizilmez.

Kullanım:  python scripts/sekiller.py
Çıktı:     makale/sekiller/*.png  (300 dpi)

Zaman ekseni "sunucunun yeniden başlatıldığı an = 0" olacak şekilde hizalanır.
Hizalama, scripts/ozet.ps1 ile aynı tanımı kullanır: kopma = bağlı cihaz sayısı en az %90'a
ulaştıktan sonra ilk kez 0 olan saniye; başlatma = kopma + kapali_sn (kayıt defterinden).
"""
from pathlib import Path

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import pandas as pd

PROJE = Path(__file__).resolve().parent.parent
SONUC = PROJE / "results"
CIKTI = PROJE.parent / "makale" / "sekiller"
CIKTI.mkdir(parents=True, exist_ok=True)

plt.rcParams.update({
    "font.family": "serif", "font.serif": ["Times New Roman", "DejaVu Serif"],
    "font.size": 10, "axes.grid": True, "grid.alpha": 0.3, "figure.dpi": 100,
})

kayit = pd.read_csv(SONUC / "deney_kayitlari.csv", encoding="utf-8-sig").set_index("kosu")


def yukle(kosu):
    """Koşu CSV'sini yükler ve zaman eksenini sunucu başlatma anına göre hizalar."""
    df = pd.read_csv(SONUC / f"{kosu}.csv")
    n = int(kayit.loc[kosu, "cihaz"])
    kapali = float(kayit.loc[kosu, "kapali_sn"])
    ulasti = df["bagli"].cummax() >= 0.9 * n
    kopma = df.loc[ulasti & (df["bagli"] == 0), "saniye"].iloc[0]
    df["t"] = df["saniye"] - (kopma + kapali)       # 0 = sunucu başlatıldı
    df["t_kopma"] = df["saniye"] - kopma            # 0 = sunucu durduruldu
    return df, n, kapali


def kaydet(fig, ad):
    yol = CIKTI / f"{ad}.png"
    fig.savefig(yol, dpi=300, bbox_inches="tight")
    plt.close(fig)
    print("yazıldı:", yol)


# Şekil A: üç bekleme kuralı, 100 cihaz, saniyedeki bağlantı denemesi (ÖN GÖZLEM, tek koşu)
fig, eksenler = plt.subplots(3, 1, figsize=(6.5, 6), sharex=True, sharey=True)
for ax, (kosu, ad) in zip(eksenler, [
    ("adim2_sabit_n100_s1", "Sabit (1 sn)"),
    ("adim2_ustel_n100_s1", "Üstel"),
    ("adim2_ustel-rastgele_n100_s1", "Rastgeleli üstel"),
]):
    df, n, kapali = yukle(kosu)
    ax.bar(df["t_kopma"], df["deneme"], width=1.0, color="#3b6ea5")
    ax.axvline(0, color="red", lw=1)
    ax.axvline(kapali, color="green", lw=1)
    ax.set_ylabel("deneme/sn")
    ax.set_title(ad, fontsize=10, loc="left")
ax.set_xlabel("Sunucu durdurulduktan sonra geçen süre (sn)   [kırmızı: durdurma, yeşil: başlatma]")
fig.suptitle("100 cihaz, saniyedeki bağlantı denemesi (ön gözlem, tek koşu)", fontsize=10)
kaydet(fig, "sekil_politika_deneme_n100")

# Şekil B: Sunucuya ad ile ve IP ile bağlanma, 5.000 cihaz, bağlı cihaz sayısı (kesinti sonrası)
fig, ax = plt.subplots(figsize=(6.5, 3.5))
for kosu, etiket, renk in [
    ("adim3_sabit_n5000_s1_v2", "Ad ile (v2): 69 sn", "#c0392b"),
    ("adim3_sabit_n5000_s1_v2b", "Ad ile (v2b): 27 sn", "#e67e22"),
    ("adim3_sabit_n5000_s1_v3mac", "Ad ile, sabit MAC (v3mac): 31 sn", "#8e44ad"),
    ("adim3_sabit_n5000_s1_v4ip", "IP ile (v4ip): 2 sn", "#27ae60"),
]:
    df, n, kapali = yukle(kosu)
    d = df[(df["t"] >= -5) & (df["t"] <= 80)]
    ax.plot(d["t"], d["bagli"], label=etiket, color=renk)
ax.axvline(0, color="black", lw=1, ls="--")
ax.set_xlabel("Sunucu başlatıldıktan sonra geçen süre (sn)")
ax.set_ylabel("Bağlı cihaz sayısı")
ax.set_title("5.000 cihaz: sunucuya ad ile ve IP ile bağlanma (her biri tek koşu)", fontsize=10)
ax.legend(fontsize=8, loc="center right")
kaydet(fig, "sekil_ad_ip_n5000")


# Şekil C: tekrarlı matris (v5): kural x N için tepe deneme ve toparlanma süresi (her nokta bir koşu)
import numpy as np
kosular = pd.read_csv(SONUC / "ozet_adim4_kosular.csv")
kurallar = [("sabit", "Sabit"), ("ustel", "Üstel"), ("ustel-rastgele", "Rastgeleli üstel")]
renk = {2000: "#3b6ea5", 5000: "#c0392b"}
fig, (a1, a2) = plt.subplots(1, 2, figsize=(7.5, 3.4))
for ax, olcut, baslik in [(a1, "tepe_kesinti", "Kesintide tepe deneme/sn"), (a2, "TTR100", "Toparlanma süresi, %100 (sn)")]:
    for j, n in enumerate([2000, 5000]):
        for i, (k, ad) in enumerate(kurallar):
            g = kosular[(kosular["kural"] == k) & (kosular["cihaz"] == n)][olcut]
            x = i + (j - 0.5) * 0.25 + np.linspace(-0.04, 0.04, len(g))
            ax.scatter(x, g, color=renk[n], s=18, label=f"N={n:,}".replace(",", ".") if i == 0 else None, zorder=3)
            ax.hlines(g.median(), i + (j - 0.5) * 0.25 - 0.08, i + (j - 0.5) * 0.25 + 0.08, color=renk[n], lw=2)
    ax.set_xticks(range(3))
    ax.set_xticklabels([a for _, a in kurallar], fontsize=8)
    ax.set_title(baslik, fontsize=10)
a1.legend(fontsize=8)
fig.suptitle("Tekrarlı deney (IP ile, 3 koşu/hücre; çizgi medyan)", fontsize=10)
fig.tight_layout()
kaydet(fig, "sekil_matris_ozet")

# Şekil D: N=5.000, tohum 2: üç kuralın saniyedeki deneme sayısı (sunucu başlatma anına göre hizalı)
fig, eksenler = plt.subplots(3, 1, figsize=(6.5, 6), sharex=True)
for ax, (k, ad) in zip(eksenler, kurallar):
    df, n, kapali = yukle(f"adim3_{k}_n5000_s2_v5")
    d = df[(df["t"] >= -70) & (df["t"] <= 60)]
    ax.bar(d["t"], d["deneme"], width=1.0, color="#3b6ea5")
    ax.axvline(0, color="green", lw=1)
    ax.axvline(-kapali, color="red", lw=1)
    ax.set_ylabel("deneme/sn")
    ax.set_title(ad, fontsize=10, loc="left")
ax.set_xlabel("Sunucu başlatıldıktan sonra geçen süre (sn)   [kırmızı: durdurma, yeşil: başlatma]")
fig.suptitle("N = 5.000, tohum 2: saniyedeki bağlantı denemesi", fontsize=10)
kaydet(fig, "sekil_deneme_n5000_v5")

# ---------------------------------------------------------------------------------------------
# Adım 5 şekilleri: canlı alarm gecikmesi (toplayıcıdan, tek yönlü saat). 0 = sunucunun yeniden başlatıldığı an.
# ---------------------------------------------------------------------------------------------
import json
import math


def alarm_serisi(kosu):
    """Canlı alarm saniyelik p99 (ms), zaman ekseni sunucu başlatma anına göre."""
    n = int(kayit.loc[kosu, "cihaz"])
    kapali = float(kayit.loc[kosu, "kapali_sn"])
    g = json.load(open(SONUC / f"{kosu}.girdi.json", encoding="utf-8"))
    sim = pd.read_csv(SONUC / f"{kosu}.csv")
    ulasti = sim["bagli"].cummax() >= math.ceil(0.9 * n)
    kopma = int(sim.loc[ulasti & (sim["bagli"] == 0), "saniye"].iloc[0])
    col = pd.read_csv(SONUC / f"{kosu}.collector.csv")
    col = col[col["sinif"] == "canli_alarm"].copy()
    col["t"] = col["mono_s"] - g["baslangic_mono_ms"] / 1000.0 - (kopma + kapali)
    return col.set_index("t")["p99_ms"]


HUCRE_RENK = {"b0": "#7f8c8d", "b1": "#c0392b", "b2": "#2c7fb8",
              "b2-w60i1000": "#27ae60", "b2-w90i500": "#8e44ad"}
HUCRE_AD = {"b0": "B0 (birikeni at)", "b1": "B1 (hemen)", "b2": "B2 (30 sn / 200 ms)",
            "b2-w60i1000": "B2 (60 sn / 1 sn)", "b2-w90i500": "B2 (90 sn / 500 ms)"}

# Şekil E: N=5.000, tohum 1, canlı alarm p99 zaman serisi, iki A kuralı yan yana (log ekseni)
fig, eksenler = plt.subplots(1, 2, figsize=(7.5, 3.4), sharey=True)
for ax, (a, ad) in zip(eksenler, [("ustel", "Üstel"), ("ustel-rastgele", "Rastgeleli üstel")]):
    for b in ["b0", "b1", "b2"]:
        s = alarm_serisi(f"adim5_{a}_{b}_n5000_s1_v6")
        s = s[(s.index >= -45) & (s.index <= 80)]
        ax.plot(s.index, s.values, color=HUCRE_RENK[b], label=HUCRE_AD[b], lw=1.2)
    ax.axvline(0, color="black", lw=1, ls="--")
    ax.axhline(100, color="gray", lw=0.8, ls=":")
    ax.set_yscale("log")
    ax.set_xlabel("Sunucu başlatıldıktan sonra geçen süre (sn)")
    ax.set_title(ad, fontsize=10)
eksenler[0].set_ylabel("Canlı alarm gecikmesi p99 (ms)")
eksenler[0].legend(fontsize=8)
fig.suptitle("N = 5.000, tohum 1: canlı alarm gecikmesi (kesikli: sunucu başlatma, noktalı: 100 ms)", fontsize=9)
fig.tight_layout()
kaydet(fig, "sekil_alarm_zaman_n5000")

# Şekil F: N=10.000, rastgeleli A, tohum 1, B0 / B1 / B2 üç ayar (v8 ve v7)
fig, ax = plt.subplots(figsize=(6.5, 3.6))
for kosu, anahtar in [
    ("adim5_ustel-rastgele_b0_n10000_s1_v8", "b0"),
    ("adim5_ustel-rastgele_b1_n10000_s1_v7", "b1"),
    ("adim5_ustel-rastgele_b2_n10000_s1_v7", "b2"),
    ("adim5_ustel-rastgele_b2-w60i1000_n10000_s1_v8", "b2-w60i1000"),
    ("adim5_ustel-rastgele_b2-w90i500_n10000_s1_v8", "b2-w90i500"),
]:
    s = alarm_serisi(kosu)
    s = s[(s.index >= -45) & (s.index <= 150)]
    ax.plot(s.index, s.values, color=HUCRE_RENK[anahtar], label=HUCRE_AD[anahtar], lw=1.2)
ax.axvline(0, color="black", lw=1, ls="--")
ax.axhline(100, color="gray", lw=0.8, ls=":")
ax.set_yscale("log")
ax.set_xlabel("Sunucu başlatıldıktan sonra geçen süre (sn)")
ax.set_ylabel("Canlı alarm gecikmesi p99 (ms)")
ax.set_title("N = 10.000, rastgeleli üstel, tohum 1 (B1 ve B2 30/200: v7; diğerleri: v8)", fontsize=9)
ax.legend(fontsize=8)
kaydet(fig, "sekil_alarm_zaman_n10000")

# Şekil G: tepe alarm p99 (her nokta bir koşu, çizgi medyan), N=5.000 ve 10.000, hücre başına
def topla(*etiketler):
    return pd.concat([pd.read_csv(SONUC / f"ozet_adim5_{e}_kosular.csv") for e in etiketler])

tum = topla("v6", "v7", "v8")
sira = [("ustel", "b0"), ("ustel", "b1"), ("ustel", "b2"), ("ustel-rastgele", "b0"), ("ustel-rastgele", "b1"),
        ("ustel-rastgele", "b2"), ("ustel-rastgele", "b2-w60i1000"), ("ustel-rastgele", "b2-w90i500")]
etiket = ["üstel\nB0", "üstel\nB1", "üstel\nB2", "rastgeleli\nB0", "rastgeleli\nB1", "rastgeleli\nB2\n30/200",
          "rastgeleli\nB2\n60/1000", "rastgeleli\nB2\n90/500"]
fig, ax = plt.subplots(figsize=(7.5, 3.8))
for j, (n, renk) in enumerate([(5000, "#3b6ea5"), (10000, "#c0392b")]):
    for i, (a, b) in enumerate(sira):
        g = tum[(tum["A"] == a) & (tum["B"] == b) & (tum["cihaz"] == n)]["tepe_p99_ms"]
        if g.empty:
            continue
        x0 = i + (j - 0.5) * 0.28
        ax.scatter(x0 + np.linspace(-0.05, 0.05, len(g)), g, color=renk, s=16, zorder=3,
                   label=f"N={n:,}".replace(",", ".") if i == 0 or (n == 10000 and i == 1) else None)
        ax.hlines(g.median(), x0 - 0.09, x0 + 0.09, color=renk, lw=2)
ax.set_yscale("log")
ax.axhline(100, color="gray", lw=0.8, ls=":")
ax.set_xticks(range(len(sira)))
ax.set_xticklabels(etiket, fontsize=7)
ax.set_ylabel("Tepe canlı alarm p99 (ms)")
ax.set_title("Sunucu açıldıktan sonra canlı alarm gecikmesinin tepesi (3 koşu/hücre; çizgi medyan)", fontsize=9)
h, l = ax.get_legend_handles_labels()
ax.legend(dict(zip(l, h)).values(), dict(zip(l, h)).keys(), fontsize=8, loc="upper right")
kaydet(fig, "sekil_alarm_tepe_hucre")

# Şekil H: bedel/kazanç, N=10.000 (medyan): tepe alarm p99'a karşı boşaltma bitişi
fig, ax = plt.subplots(figsize=(5.8, 3.8))
n10 = tum[tum["cihaz"] == 10000]
for (a, b), g in n10.groupby(["A", "B"]):
    if b == "b0":
        continue
    ax.scatter(g["bosaltma_bitis_sn"].median(), g["tepe_p99_ms"].median(), s=40,
               color=HUCRE_RENK[b], marker="o" if a == "ustel-rastgele" else "s", zorder=3)
    ax.annotate(f"{'rastg.' if a == 'ustel-rastgele' else 'üstel'} {HUCRE_AD[b]}", (g["bosaltma_bitis_sn"].median(), g["tepe_p99_ms"].median()),
                fontsize=7, xytext=(4, -10) if (a == "ustel-rastgele" and b == "b2") else (4, 4), textcoords="offset points")
ax.set_yscale("log")
ax.set_xlabel("Boşaltmanın bitişi (sunucu açıldıktan sonra, sn; medyan)")
ax.set_ylabel("Tepe canlı alarm p99 (ms; medyan)")
ax.set_title("N = 10.000: gecikme ve boşaltma süresi arasındaki denge (B0 çizilmedi, veri atar)", fontsize=8)
ax.set_xlim(20, 200)
kaydet(fig, "sekil_alarm_denge_n10000")
