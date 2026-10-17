// Canlı simülasyon modelini (web/model.js) ölçümlerimize kalibre eder.
//
// Kullanım:   node scripts/model_kalibrasyon.mjs mosquitto|emqx2|emqx4   (parametre ara, web/model_param_<ad>.json yaz)
//             node scripts/model_kalibrasyon.mjs birlestir               (parçaları web/model_param.json ve model_artik.json'a birleştir, tablo yaz)
//             node scripts/model_kalibrasyon.mjs degerlendir             (mevcut model_param.json ile tabloyu yeniden hesapla)
// Girdi:      web/veri.js (ölçüm medyanları: scripts/web_uret.py üretir), web/model.js
// Çıktı:      web/model_param.json (sunucu başına parametreler), web/model_artik.json (hücre başına model vs ölçüm)
import { createRequire } from "module";
import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const require = createRequire(import.meta.url);
const __dir = path.dirname(fileURLToPath(import.meta.url));
const WEB = path.join(__dir, "..", "web");
const M = require(path.join(WEB, "model.js"));

// ---------------------------------------------------------------- ölçüm hedefleri
function veriYukle() {
  const js = fs.readFileSync(path.join(WEB, "veri.js"), "utf-8");
  const w = {}; new Function("window", js)(w);
  return w.VERI;
}
const med = (a) => { const s = a.filter((x) => x != null && !Number.isNaN(x)).sort((x, y) => x - y); if (!s.length) return null; const k = s.length >> 1; return s.length % 2 ? s[k] : (s[k - 1] + s[k]) / 2; };

function bAyrisim(B) {   // "b2-w90i500" -> {B:"b2", W:90, I:500}
  const m = /^(b\d)(?:-w(\d+)i(\d+))?$/.exec(B || "");
  if (!m) return { B: "b0", W: 30, I: 200 };
  return { B: m[1], W: m[2] ? +m[2] : 30, I: m[3] ? +m[3] : 200 };
}
function sunucuAd(r) { return r.sunucu === "emqx" ? (r.cpu >= 4 ? "emqx4" : "emqx2") : "mosquitto"; }

function hedefler(V) {
  const g = new Map();
  for (const r of V.runlar) {
    if (!r.ozet) continue;
    const k = [r.aile, sunucuAd(r), r.tls ? 1 : 0, r.N, r.A, r.B].join("|");
    if (!g.has(k)) g.set(k, { aile: r.aile, sunucu: sunucuAd(r), tls: !!r.tls, N: r.N, A: r.A, B: r.B, runs: [] });
    g.get(k).runs.push(r.ozet);
  }
  const out = [];
  for (const h of g.values()) {
    const o = h.runs;
    if (h.aile === "adim5") {
      out.push(Object.assign(h, { runs: undefined, n: o.length, tepe: med(o.map((x) => x.tepe)), esik: med(o.map((x) => x.esik)), bosaltma: med(o.map((x) => x.bosaltma)), yasP50: med(o.map((x) => x.yas_p50)), yasMax: med(o.map((x) => x.yas_max)) }));
    } else {
      out.push(Object.assign(h, { runs: undefined, n: o.length, ttr50: med(o.map((x) => x.ttr50)), ttr100: med(o.map((x) => x.ttr100)), hataSonra: med(o.map((x) => x.hata_sonra)), tepeKes: med(o.map((x) => x.tepe_kesinti)) }));
    }
  }
  return out;
}

// ---------------------------------------------------------------- model koşusu
function hucreKos(h, p, seed, dt) {
  const bb = bAyrisim(h.B);
  const cfg = { N: h.N, A: h.A, B: bb.B, W: bb.W, I: bb.I, tls: h.tls, seed, dt, is: h.aile === "adim5" };
  const izle = h.aile === "adim5" ? 150 : 120;
  return M.kos(cfg, p, { kesinti: 60, izle, kes: 40 }).ozet;
}
const lg = (x, y, e) => Math.abs(Math.log10((x + e) / (y + e)));
function kayip(h, o) {
  let k = 0;
  if (h.aile === "adim5") {
    k += lg(o.tepe, h.tepe, 8) * 1.0;
    if (h.esik != null) k += Math.min(1, Math.abs(o.esik - h.esik) / (h.esik + 10)) * 0.25;
    if (h.bosaltma != null && o.bosaltma != null) k += Math.min(1, Math.abs(o.bosaltma - h.bosaltma) / h.bosaltma) * 0.4;
    else if (h.bosaltma != null) k += 0.4;
    if (h.yasP50 != null && o.yasP50 != null) k += Math.min(1, Math.abs(o.yasP50 - h.yasP50) / h.yasP50) * 0.3;
  } else {
    if (h.ttr100 != null) k += lg(o.ttr100 ?? 150, h.ttr100, 1) * 1.0;
    if (h.ttr50 != null) k += lg(o.ttr50 ?? 150, h.ttr50, 1) * 0.5;
    if (h.hataSonra != null) k += lg(o.hataSonra, h.hataSonra, 30) * 0.3;
  }
  return k;
}

// ---------------------------------------------------------------- arama
const SINIR = { m: [1.5e-5, 1.5e-4], cConn: [1e-5, 1e-3], h: [2e-4, 6e-3], cSync: [3e-5, 2e-3], bayat: [0.02, 1], L0: [0.004, 0.05], L0s: [0, 0.08], pay: [0.01, 0.4], gam: [0, 1.5] };
const BASLA = {
  mosquitto: { m: 3.8e-5, cConn: 1.0e-4, h: 1.6e-3, cSync: 3.0e-4, bayat: 0.5, L0: 0.008, L0s: 0.045, pay: 0.1, gam: 0.3 },
  emqx2: { m: 3.5e-5, cConn: 2.0e-4, h: 1.6e-3, cSync: 4.0e-4, bayat: 0.5, L0: 0.01, L0s: 0.05, pay: 0.1, gam: 0.3 },
  emqx4: { m: 3.0e-5, cConn: 2.0e-4, h: 1.6e-3, cSync: 3.0e-4, bayat: 0.5, L0: 0.01, L0s: 0.05, pay: 0.1, gam: 0.3 },
};
// Sunumda en çok kullanılan hücrelere (Mosquitto, N = 5.000) ağırlık: tutmaları daha önemli
const agirlik = (h) => (h.sunucu === "mosquitto" && h.N === 5000 && h.aile === "adim5" ? 2.5 : 1);
function toplamKayip(hs, p, seed, dt) { let k = 0; for (const h of hs) k += agirlik(h) * kayip(h, hucreKos(h, p, seed, dt)); return k; }

function ara(ad, hs) {
  let p = Object.assign({}, BASLA[ad]);
  const onceki = path.join(WEB, `model_param_${ad}.json`);
  if (process.argv[3] === "devam" && fs.existsSync(onceki)) p = Object.assign(p, JSON.parse(fs.readFileSync(onceki, "utf-8")));   // önceki sonuçtan devam
  const kullan = ad === "mosquitto" ? Object.keys(SINIR) : Object.keys(SINIR).filter((k) => k !== "h");   // EMQX'te TLS yok
  let en = toplamKayip(hs, p, 1, 0.1);
  console.log(`[${ad}] başlangıç kayıp ${en.toFixed(3)} (${hs.length} hücre)`);
  for (let tur = 0; tur < 4; tur++) {
    const adim = [2.2, 1.6, 1.3, 1.15][tur];
    let iyilesti = false;
    for (const k of kullan) {
      for (const c of [p[k] * adim, p[k] / adim]) {
        const [lo, hi] = SINIR[k];
        if (c < lo || c > hi) continue;
        const q = Object.assign({}, p, { [k]: c });
        const kay = toplamKayip(hs, q, 1, 0.1);
        if (kay < en - 1e-9) { en = kay; p = q; iyilesti = true; }
      }
    }
    console.log(`[${ad}] tur ${tur + 1}: kayıp ${en.toFixed(3)}  ${JSON.stringify(p)}`);
    if (!iyilesti && tur >= 2) break;
  }
  return p;
}

// ---------------------------------------------------------------- değerlendirme tablosu
function tablo(sunucular, hs) {
  const satirlar = [];
  for (const h of hs) {
    const p = sunucular[h.sunucu]; if (!p) continue;
    const os = [1, 2, 3].map((s) => hucreKos(h, p, s, 0.05));
    const med3 = (f) => med(os.map(f));
    const m = h.aile === "adim5"
      ? { tepe: med3((o) => o.tepe), esik: med3((o) => o.esik), bosaltma: med3((o) => o.bosaltma), yasP50: med3((o) => o.yasP50) }
      : { ttr100: med3((o) => o.ttr100), ttr50: med3((o) => o.ttr50), hataSonra: med3((o) => o.hataSonra) };
    satirlar.push({ h, m });
  }
  return satirlar;
}
function artikRapor(satirlar) {
  const rapor = [];
  for (const { h, m } of satirlar) {
    let oran, tam;
    if (h.aile === "adim5") {
      oran = (m.tepe + 8) / (h.tepe + 8);
      tam = Math.max(oran, 1 / oran) <= 2 && (h.bosaltma == null || m.bosaltma == null || Math.abs(m.bosaltma - h.bosaltma) / h.bosaltma <= 0.25);
    } else {
      oran = ((m.ttr100 ?? 150) + 1) / (h.ttr100 + 1);
      tam = Math.max(oran, 1 / oran) <= 1.5;
    }
    rapor.push({ aile: h.aile, sunucu: h.sunucu, tls: h.tls, N: h.N, A: h.A, B: h.B, olcum: h, model: m, oran, tuttu: tam });
  }
  return rapor;
}

// ---------------------------------------------------------------- ana
const komut = process.argv[2] || "mosquitto";
const V = veriYukle();
const hs = hedefler(V);
if (BASLA[komut]) {
  const kume = hs.filter((h) => h.sunucu === komut);
  const p = ara(komut, kume);
  fs.writeFileSync(path.join(WEB, `model_param_${komut}.json`), JSON.stringify(p, null, 1));
  console.log(`yazıldı: model_param_${komut}.json`);
} else if (komut === "birlestir" || komut === "degerlendir") {
  const sunucular = {};
  if (komut === "birlestir") for (const ad of Object.keys(BASLA)) sunucular[ad] = JSON.parse(fs.readFileSync(path.join(WEB, `model_param_${ad}.json`), "utf-8"));
  else Object.assign(sunucular, JSON.parse(fs.readFileSync(path.join(WEB, "model_param.json"), "utf-8")));
  // EMQX'te h (TLS) kullanılmaz; Mosquitto değeri kopyalanır (EMQX TLS'siz denendi, UI'da TLS seçeneği EMQX için kapalı)
  const satirlar = tablo(sunucular, hs);
  const rapor = artikRapor(satirlar);
  fs.writeFileSync(path.join(WEB, "model_param.json"), JSON.stringify(sunucular, null, 1));
  fs.writeFileSync(path.join(WEB, "model_artik.json"), JSON.stringify(rapor));
  let tutan = 0;
  for (const r of rapor) {
    if (r.tuttu) tutan++;
    const o = r.olcum, m = r.model;
    if (r.aile === "adim5") console.log(`${r.tuttu ? "OK " : "XX "} ${r.sunucu.padEnd(9)} ${r.tls ? "TLS " : "    "} N=${String(r.N).padEnd(5)} ${r.A.padEnd(14)} ${r.B.padEnd(11)} tepe ölçüm ${String(Math.round(o.tepe)).padStart(6)} model ${String(Math.round(m.tepe)).padStart(6)} | bosaltma ${o.bosaltma == null ? "  -  " : o.bosaltma.toFixed(0).padStart(5)}/${m.bosaltma == null ? "  -  " : m.bosaltma.toFixed(0).padStart(5)} | yaş ${o.yasP50 == null ? " - " : o.yasP50.toFixed(0).padStart(3)}/${m.yasP50 == null ? " - " : String(m.yasP50).padStart(3)}`);
    else console.log(`${r.tuttu ? "OK " : "XX "} ${r.sunucu.padEnd(9)} ${r.tls ? "TLS " : "    "} N=${String(r.N).padEnd(5)} ${r.A.padEnd(14)} (bağlanma)  ttr100 ölçüm ${o.ttr100} model ${m.ttr100} | ttr50 ${o.ttr50}/${m.ttr50} | hata ${o.hataSonra}/${Math.round(m.hataSonra)}`);
  }
  console.log(`\nTutan hücre: ${tutan}/${rapor.length} (ölçüt: tepe 2 kat içinde ve boşaltma ±%25; bağlanma için TTR100 1,5 kat içinde)`);
}
