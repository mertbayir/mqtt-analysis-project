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