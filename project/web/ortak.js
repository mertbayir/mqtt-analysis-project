"use strict";
const $ = (id) => document.getElementById(id);
const fmt = (n, d = 0) => n == null ? "–" : Number(n).toLocaleString("tr-TR", { maximumFractionDigits: d });
const med = (a) => { const s = a.filter((x) => x != null).sort((x, y) => x - y); if (!s.length) return null; const m = s.length >> 1; return s.length % 2 ? s[m] : (s[m - 1] + s[m]) / 2; };

const SVGNS = "http://www.w3.org/2000/svg";
function el(name, attrs, parent) { const e = document.createElementNS(SVGNS, name); for (const k in attrs) e.setAttribute(k, attrs[k]); if (parent) parent.appendChild(e); return e; }
function css(v) { return getComputedStyle(document.documentElement).getPropertyValue(v).trim(); }
function makeChart(svg, o) {
  // o: {t0, n, series:[{y, color, dash}], log, ymin, ymax, ticks, W, H}
  svg.innerHTML = "";
  const W = o.W, H = o.H, L = 50, R = 8, T = 8, Bm = 22, pw = W - L - R, ph = H - T - Bm;
  const x = (i) => L + (i / Math.max(1, o.n - 1)) * pw;
  const sc = o.log ? (v) => Math.log10(Math.max(v, o.ymin)) : (v) => v;
  const y = (v) => T + ph - ((sc(v) - sc(o.ymin)) / (sc(o.ymax) - sc(o.ymin))) * ph;
  const grid = css("--line"), mut = css("--muted");
  // y ızgarası
  o.ticks.forEach((tv) => {
    el("line", { x1: L, x2: W - R, y1: y(tv), y2: y(tv), stroke: grid, "stroke-width": 1 }, svg);
    const tx = el("text", { x: L - 6, y: y(tv) + 4, "text-anchor": "end" }, svg); tx.textContent = fmt(tv);
  });
  // x ekseni
  const step = o.n > 200 ? 50 : o.n > 120 ? 40 : 20;
  for (let tt = Math.ceil(o.t0 / step) * step; tt < o.t0 + o.n; tt += step) {
    const xi = x(tt - o.t0);
    el("line", { x1: xi, x2: xi, y1: T + ph, y2: T + ph + 4, stroke: mut }, svg);
    const tx = el("text", { x: xi, y: H - 6, "text-anchor": "middle" }, svg); tx.textContent = tt;
  }
  if (o.t0 < 0) el("line", { x1: x(-o.t0), x2: x(-o.t0), y1: T, y2: T + ph, stroke: css("--ok"), "stroke-width": 1.5, "stroke-dasharray": "4 3" }, svg);
  if (o.tdur != null && o.tdur > o.t0) {
    el("rect", { x: x(o.tdur - o.t0), y: T, width: Math.max(0, x(-o.t0) - x(o.tdur - o.t0)), height: ph, fill: css("--bad"), opacity: .07 }, svg);
    el("line", { x1: x(o.tdur - o.t0), x2: x(o.tdur - o.t0), y1: T, y2: T + ph, stroke: css("--bad"), "stroke-width": 1.5, "stroke-dasharray": "4 3" }, svg);
  }
  (o.bands || []).forEach((b) => el("rect", { x: x(b[0]), y: T, width: Math.max(0, x(b[1]) - x(b[0])), height: ph, fill: b[2], opacity: .09 }, svg));
  if (o.hline) el("line", { x1: L, x2: W - R, y1: y(o.hline), y2: y(o.hline), stroke: mut, "stroke-dasharray": "2 3" }, svg);
  o.series.forEach((s) => {
    let d = "", pen = false;
    s.y.forEach((v, i) => {
      if (v == null) { pen = false; return; }
      d += (pen ? "L" : "M") + x(i).toFixed(1) + " " + y(v).toFixed(1); pen = true;
    });
    el("path", { d, fill: "none", stroke: s.color, "stroke-width": 1.8, "stroke-dasharray": s.dash || "", "stroke-linejoin": "round" }, svg);
  });
  const cur = el("line", { x1: L, x2: L, y1: T, y2: T + ph, stroke: css("--ink"), "stroke-width": 1.2, opacity: .7 }, svg);
  return { setCursor(i) { const xi = x(i); cur.setAttribute("x1", xi); cur.setAttribute("x2", xi); } };
}

