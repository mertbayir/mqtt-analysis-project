/* Sunucu kesintisi sonrası toplu yeniden bağlanma: BASİT MODEL (ölçüm değildir).
 *
 * Tek FIFO akışkan kuyruk (sunucu CPU'su) + N cihaz durum makinesi. Parametreler (m, cConn, h, cSync, bayat, L0)
 * ölçümlerimize kalibre edilir (scripts/model_kalibrasyon.mjs). Aynı kod tarayıcıda (window.MQTTModel) ve Node'da çalışır.
 *
 * Cihaz davranışı src/Simulator/Device.cs ile aynıdır: bekleme kuralları (sabit, üstel, rastgeleli üstel), 5 sn bağlantı
 * zaman aşımı, ilk bağlanma 20 sn'ye yayılı, yeniden bağlanınca bağlantı sonrası politika (B0 at; B1 hemen; B2 U(0,W)
 * gecikme + aralıklı; B3 alarmlar hemen, vital B2 gibi), eşitleme (iki geçiş, 10 sn zaman aşımı).
 *
 * Modellenmeyenler: mesaj kaybı, aykırı koşular, bloklar arası değişkenlik, ağ gecikmesi, bellek.
 */
(function (root, factory) {
  if (typeof module === "object" && module.exports) module.exports = factory();
  else root.MQTTModel = factory();
})(typeof self !== "undefined" ? self : this, function () {
  "use strict";

  function mulberry32(a) {
    return function () {
      a |= 0; a = (a + 0x6D2B79F5) | 0;
      let t = Math.imul(a ^ (a >>> 15), 1 | a);
      t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
      return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };
  }

  const ST = { BAGLI: 0, BEKLIYOR: 1, DENIYOR: 2, ILK: 3 };
  const E = { DENEME: 0, BASARISIZ: 1, BASARILI: 2, SYNC1: 3, SYNC2: 4, FLUSH: 5 };
  const ZAMAN_ASIMI = 5, SYNC_ZAMAN_ASIMI = 10, VITAL = 0.5, ALARM = 0.1;

  // Varsayılan (kalibrasyondan önce) parametreler
  const VARSAYILAN = { m: 4.0e-5, cConn: 1.0e-4, h: 1.6e-3, cSync: 2.4e-4, bayat: 0.5, L0: 0.01, L0s: 0.04, pay: 0.1, gam: 0.3 };   // pay: el sıkışma yoğunken mesajların CPU payı; gam: N > 5.000'de mesaj maliyetinin artışı   // taban gecikme = L0 + L0s * (bağlı / 10.000)

  class Sim {
    /* cfg: {N, A, B, tls, W (sn), I (ms), seed, dt, ilkYayilim, kesZamani, acZamani}; p: parametre kümesi */
    constructor(cfg, p, hazirla) {
      this.cfg = Object.assign({ N: 5000, A: "ustel", B: "b1", tls: false, W: 30, I: 200, seed: 1, dt: 0.05, ilkYayilim: 20, is: true }, cfg);   // is=false: yalnızca bağlanma (iş yükü yok)
      this.p = Object.assign({}, VARSAYILAN, p || {});
      const N = this.cfg.N;
      this.rng = mulberry32(this.cfg.seed * 7919 + 13);
      this.t = 0; this.W = 0; this.H = 0; this.up = true;
      this.st = new Int8Array(N); this.beklemeNo = new Int16Array(N);
      this.tDisc = new Float64Array(N).fill(-1); this.tDeneme = new Float64Array(N);
      this.deneme_ok = new Uint8Array(N); this.gorulmus = new Uint8Array(N);
      this.carryV = new Float64Array(N); this.carryA = new Float64Array(N);
      this.flushVar = new Uint8Array(N);   // cihaz şu an boşaltıyor mu (çizim için)
      this.bucket = new Map(); this.dt = this.cfg.dt;
      this.bagli = 0; this.flushers = [];
      // saniyelik seriler
      this.seri = { t: [], bagli: [], deneme: [], hata: [], alarm: [], kuyruk: [], yuk: [], sync: [], flushing: [] };
      this._sn = { deneme: 0, hata: 0, alarmMax: 0, kuyrukMax: 0, is: 0, sync: 0, sonSn: 0, steps: 0 };
      this.syncHata = 0; this.syncSayisi = 0; this.atilan = 0; this.bosaltmaSon = 0;
      this.yasBin = new Float64Array(2000); this.yasMax = 0; this.yasToplam = 0;
      this.kesTarih = null; this.acTarih = null;
      // ilk bağlanma: [0, yayılım] sn
      for (let i = 0; i < N; i++) {
        this.st[i] = ST.ILK;
        this._planla(this.rng() * this.cfg.ilkYayilim, E.DENEME, i);
      }
      if (this.cfg.kesZamani != null) this.kesTarih = this.cfg.kesZamani;
      if (this.cfg.acZamani != null) this.acTarih = this.cfg.acZamani;
    }

    _planla(zaman, tip, i) {
      const cur = Math.round(this.t / this.dt);
      const b = Math.max(cur + 1, Math.round(zaman / this.dt));   // en erken bir sonraki adım (işlenen kovaya ekleme olmasın)
      let a = this.bucket.get(b);
      if (!a) { a = []; this.bucket.set(b, a); }
      a.push(tip, i);
    }

    _bekleme(i) {
      // beklemeNo[i] bu kesintide kaçıncı bekleme (1'den)
      const n = this.beklemeNo[i];
      const A = this.cfg.A;
      if (A === "sabit") return 1;
      const W = Math.min(30, Math.pow(2, n - 1));
      if (A === "ustel") return W;
      if (n === 1) return this.rng() * 5;                 // RFC 6455 §7.2.3: ilk bekleme U(0,5)
      return W * (0.5 + 0.5 * this.rng());                // RFC 5626 §4.5: U(0.5W, W)
    }

    kes() {   // sunucuyu aniden kes
      if (!this.up) return;
      this.up = false; this.W = 0; this.H = 0; this.flushers.length = 0;
      for (let i = 0; i < this.cfg.N; i++) {
        this.flushVar[i] = 0;
        if (this.st[i] === ST.BAGLI) {
          this.st[i] = ST.BEKLIYOR; this.beklemeNo[i] = 1; this.tDisc[i] = this.t; this.bagli--;
          this._planla(this.t + this._bekleme(i), E.DENEME, i);
        }
      }
      this.bagli = Math.max(0, this.bagli);
    }
    ac() { if (this.up) return; this.up = true; this.W = 0; this.H = 0; this.tAc = this.t; }
    // mesaj sınıfının etkin hızı: el sıkışma/bağlantı işi varken CPU'nun alpha payı ona gider (iki sınıflı paylaşım)
    _mu() { return this.H > 1e-9 ? Math.max(0.005, this.p.pay) : 1; }
    _hEtkin() { return this.W > 1e-9 ? Math.max(0.05, 1 - this.p.pay) : 1; }
    // N büyüdükçe mesaj başına maliyet artar (bağlantı sayısına bağlı ek yük)
    _m() { return this.p.m * (1 + this.p.gam * Math.max(0, this.cfg.N - 5000) / 5000); }
    _hEkle(is) { this.H += is; this._sn.is += is; }

    _isEkle(is) { this.W += is; this._sn.is += is; }

    _flushEkle(i, nbV, nbA, tDisc, tBag) {
      // i cihazının birikeni (nbV vital + nbA alarm) kuyruğa girer
      const cfg = this.cfg, m = this.p.m;
      const nb = nbV + nbA;
      if (nb <= 0) return;
      if (cfg.B === "b1") {   // hepsi hemen: tek yığın
        this._yigin(nb, nbA, tDisc, tBag, this.t);
        this.flushVar[i] = 1;
        this._planla(this.t + 0.5, E.FLUSH, i);   // gösterim için kısa süre "boşaltıyor"
      } else {                // b2 / b3: aralıklı
        this.flushers.push({ i, rem: nb, tot: nb, pA: nbA / nb, tDisc, tBag, ts: this.t, rate: 1000 / cfg.I });
        this.flushVar[i] = 1;
      }
    }

    _yigin(nb, nbA, tDisc, tBag, ts) {
      // nb mesajlık yığın şu an kuyruğa girer (FIFO): tamamlanma ts+W .. ts+W+b
      const m = this._m(), b = nb * m, W0 = this.W / this._mu();
      this._isEkle(b);
      if (nbA > 0) {
        for (const u of [0.5]) {
          const tdel = ts + W0 + b * u, plan = tDisc + u * (tBag - tDisc);
          this._yasKaydet(tdel - plan, nbA);
        }
        this._yasMaxGuncelle(ts + W0 - tDisc);
        this._yasMaxGuncelle(ts + W0 + b - tBag);
      }
      this.bosaltmaSon = Math.max(this.bosaltmaSon, ts + W0 + b / this._mu());
    }
    _yasKaydet(yas, n) { const k = Math.max(0, Math.min(1999, Math.round(yas))); this.yasBin[k] += n; this.yasToplam += n; }
    _yasMaxGuncelle(y) { if (y > this.yasMax) this.yasMax = y; }

    _basarili(i) {
      // bağlantı kuruldu
      const cfg = this.cfg, p = this.p;
      this.st[i] = ST.BAGLI; this.bagli++; this.beklemeNo[i] = 0;
      if (!this.gorulmus[i]) { this.gorulmus[i] = 1; return; }       // ilk açılış: bağlantı sonrası iş yok
      if (!cfg.is) return;                                            // iş yükü kapalı: birikme ve boşaltma yok
      const dis = this.tDisc[i] >= 0 ? this.t - this.tDisc[i] : 0;
      const nbV = this.carryV[i] + VITAL * dis, nbA = this.carryA[i] + ALARM * dis;
      this.carryV[i] = 0; this.carryA[i] = 0;
      const tDisc = this.tDisc[i], tBag = this.t;
      if (cfg.B === "b0") { this.atilan += nbV + nbA; return; }
      if (cfg.B === "b3") {
        // alarmlar hemen, vital B2 gibi
        this._yigin(nbA, nbA, tDisc, tBag, this.t);
        this._planla(this.t + this.rng() * cfg.W, E.SYNC1, i);
        this.carryV[i] = nbV; this.carryA[i] = 0;   // vital, sync sonrası boşalır (carry olarak saklanır)
        this._tDiscSakla(i, tDisc, tBag);
        return;
      }
      const gecikme = cfg.B === "b2" ? this.rng() * cfg.W : 0;
      this.carryV[i] = nbV; this.carryA[i] = nbA; this._tDiscSakla(i, tDisc, tBag);
      this._planla(this.t + gecikme, E.SYNC1, i);
    }
    _tDiscSakla(i, tDisc, tBag) { (this._bekleyen || (this._bekleyen = new Map())).set(i, [tDisc, tBag]); }

    _olay(tip, i) {
      const cfg = this.cfg, p = this.p, t = this.t;
      switch (tip) {
        case E.DENEME: {
          if (this.st[i] === ST.BAGLI) return;
          this.st[i] = ST.DENIYOR; this.tDeneme[i] = t; this._sn.deneme++;
          if (!this.up) { this._planla(t + ZAMAN_ASIMI, E.BASARISIZ, i); this.deneme_ok[i] = 0; return; }
          const maliyet = p.cConn + (cfg.tls ? p.h : 0);
          const gec = (this.H + maliyet) / this._hEtkin();
          if (gec <= ZAMAN_ASIMI) { this._hEkle(maliyet); this.deneme_ok[i] = 1; this._planla(t + gec, E.BASARILI, i); }
          else { this._hEkle(maliyet * p.bayat); this.deneme_ok[i] = 0; this._planla(t + ZAMAN_ASIMI, E.BASARISIZ, i); }
          return;
        }
        case E.BASARISIZ: {
          if (this.st[i] !== ST.DENIYOR) return;
          this._sn.hata++; this.st[i] = ST.BEKLIYOR; this.beklemeNo[i]++;
          this._planla(t + this._bekleme(i), E.DENEME, i);
          return;
        }
        case E.BASARILI: {
          if (this.st[i] !== ST.DENIYOR) return;
          if (!this.up) { this.st[i] = ST.BEKLIYOR; this.beklemeNo[i]++; this._planla(t + this._bekleme(i), E.DENEME, i); return; }
          this._basarili(i);
          return;
        }
        case E.SYNC1: {
          if (this.st[i] !== ST.BAGLI || !this.up) return;
          this.syncSayisi++;
          const d1 = (this.W + p.cSync) / this._mu(); this._isEkle(p.cSync);
          this._planla(t + d1, E.SYNC2, i); this._syncT0 = this._syncT0 || new Map(); this._syncT0.set(i, t);
          return;
        }
        case E.SYNC2: {
          if (this.st[i] !== ST.BAGLI || !this.up) return;
          const t0 = this._syncT0.get(i), d2 = (this.W + p.cSync) / this._mu(); this._isEkle(p.cSync);
          const toplam = (t - t0) + d2;
          this._sn.sync = Math.max(this._sn.sync, Math.min(toplam, SYNC_ZAMAN_ASIMI));
          let bekle = toplam;
          if (toplam > SYNC_ZAMAN_ASIMI) { this.syncHata++; bekle = SYNC_ZAMAN_ASIMI - (t - t0); }
          this._planla(t + Math.max(0, bekle), E.FLUSH, i + 0.5);   // .5: gerçek boşaltma başlangıcı
          return;
        }
        case E.FLUSH: {
          // gösterim bayrağını kapat (b1) ya da sync sonrası boşaltma başlat
          return;
        }
      }
    }

    _flushBaslat(i) {
      if (this.st[i] !== ST.BAGLI || !this.up) return;
      const bk = this._bekleyen && this._bekleyen.get(i);
      if (!bk) return;
      this._bekleyen.delete(i);
      const nbV = this.carryV[i], nbA = this.carryA[i];
      this.carryV[i] = 0; this.carryA[i] = 0;
      this._flushEkle(i, nbV, nbA, bk[0], bk[1]);
    }

    /* tek zaman adımı */
    adim() {
      const dt = this.dt, p = this.p;
      this.t += dt;
      if (this.kesTarih != null && this.t >= this.kesTarih) { this.kes(); this.kesTarih = null; }
      if (this.acTarih != null && this.t >= this.acTarih) { this.ac(); this.acTarih = null; }
      // olaylar
      const b = Math.round(this.t / dt);
      const lst = this.bucket.get(b);
      if (lst) {
        this.bucket.delete(b);
        for (let k = 0; k < lst.length; k += 2) {
          const tip = lst[k], id = lst[k + 1];
          if (tip === E.FLUSH) {
            if (Number.isInteger(id)) this.flushVar[id] = 0;
            else this._flushBaslat(id - 0.5);
          } else this._olay(tip, id);
        }
      }
      // canlı trafik
      if (this.cfg.is && this.up && this.bagli > 0) this._isEkle(this.bagli * (VITAL + ALARM) * dt * this._m());
      // aralıklı boşaltma (b2 / b3)
      if (this.up && this.flushers.length) {
        const m = this._m(), kalan = [];
        for (let k = 0; k < this.flushers.length; k++) {
          const f = this.flushers[k];
          const n = Math.min(f.rem, f.rate * dt);
          if (this.st[f.i] !== ST.BAGLI) { this.carryV[f.i] += f.rem * (1 - f.pA); this.carryA[f.i] += f.rem * f.pA; this.flushVar[f.i] = 0; continue; }
          const oncesi = this.W / this._mu();
          this._isEkle(n * m);
          const q = 1 - (f.rem - n / 2) / f.tot;
          const nA = n * f.pA;
          if (nA > 0) {
            const tdel = this.t + oncesi, plan = f.tDisc + q * (f.tBag - f.tDisc);
            this._yasKaydet(tdel - plan, nA); this._yasMaxGuncelle(tdel - plan);
          }
          this.bosaltmaSon = Math.max(this.bosaltmaSon, this.t + oncesi + n * m / this._mu());
          f.rem -= n;
          if (f.rem > 1e-6) kalan.push(f); else this.flushVar[f.i] = 0;
        }
        this.flushers = kalan;
      }
      // kuyruk boşalır (kapasite 1 iş-sn / sn)
      if (this.up) {
        let kalan = dt;
        if (this.H > 1e-12) {
          const hs = Math.min(this.H, (this.W > 1e-9 ? 1 - p.pay : 1) * dt);
          this.H -= hs; kalan = dt - hs;
        }
        this.W = Math.max(0, this.W - kalan);
      }
      // çıktı: canlı alarm gecikmesi = mesaj kuyruğu gecikmesi + taban
      if (this.up && this.bagli > 0) {
        const kg = this.W / this._mu();
        const d = kg + p.L0 + p.L0s * this.bagli / 10000 + this._m(); if (d > this._sn.alarmMax) this._sn.alarmMax = d;
        if (kg > this._sn.kuyrukMax) this._sn.kuyrukMax = kg;
      }
      // saniye sonu
      const sn = Math.floor(this.t);
      if (sn > this._sn.sonSn) {
        const s = this._sn;
        this.seri.t.push(sn); this.seri.bagli.push(this.bagli); this.seri.deneme.push(s.deneme); this.seri.hata.push(s.hata);
        this.seri.alarm.push(this.up && this.bagli > 0 ? s.alarmMax * 1000 * (1 + 0.03 * (this.rng() - 0.5)) : null);
        this.seri.kuyruk.push(s.kuyrukMax); this.seri.yuk.push(Math.min(1, s.is)); this.seri.sync.push(s.sync);
        this.seri.flushing.push(this.flushers.length);
        s.deneme = 0; s.hata = 0; s.alarmMax = 0; s.kuyrukMax = 0; s.is = 0; s.sync = 0; s.sonSn = sn;
      }
    }

    ilerle(saniye) { const n = Math.round(saniye / this.dt); for (let k = 0; k < n; k++) this.adim(); }
    yasYuzdelik(q) {
      if (this.yasToplam <= 0) return null;
      let c = 0; const hedef = q * this.yasToplam;
      for (let k = 0; k < this.yasBin.length; k++) { c += this.yasBin[k]; if (c >= hedef) return k; }
      return this.yasBin.length;
    }
  }

  /* Ölçüm zaman çizelgesiyle koşu: 40. sn kes, 40+kesinti aç, sonra sureSonra sn izle. Özet ölçütleri döner. */
  function kos(cfg, p, opts) {
    opts = Object.assign({ kesinti: 60, izle: 120, kes: 40 }, opts || {});
    const sim = new Sim(Object.assign({}, cfg, { kesZamani: opts.kes, acZamani: opts.kes + opts.kesinti }), p);
    sim.ilerle(opts.kes + opts.kesinti + opts.izle);
    return { sim, ozet: ozetle(sim, opts) };
  }

  function ozetle(sim, opts) {
    const s = sim.seri, acT = opts.kes + opts.kesinti;
    let tepe = 0, esik = 0, taban = [], ttr50 = null, ttr95 = null, ttr100 = null, hataSonra = 0, tepeKes = 0;
    const N = sim.cfg.N;
    for (let k = 0; k < s.t.length; k++) {
      const t = s.t[k];
      if (t > 25 && t < opts.kes - 1 && s.alarm[k] != null) taban.push(s.alarm[k]);
      if (t >= opts.kes && t <= acT && s.deneme[k] > tepeKes) tepeKes = s.deneme[k];
      if (t > acT) {
        if (s.alarm[k] != null) { if (s.alarm[k] > tepe) tepe = s.alarm[k]; if (s.alarm[k] > 100) esik++; }
        hataSonra += s.hata[k];
        const r = t - acT;
        if (ttr50 == null && s.bagli[k] >= 0.5 * N) ttr50 = r;
        if (ttr95 == null && s.bagli[k] >= 0.95 * N) ttr95 = r;
        if (ttr100 == null && s.bagli[k] >= N) ttr100 = r;
      }
    }
    taban.sort((a, b) => a - b);
    return {
      tepe, esik, taban: taban.length ? taban[taban.length >> 1] : null, ttr50, ttr95, ttr100, hataSonra, tepeKes,
      bosaltma: sim.bosaltmaSon > acT ? sim.bosaltmaSon - acT : null,
      yasP50: sim.yasYuzdelik(0.5), yasMax: sim.yasMax > 0 ? sim.yasMax : null,
      syncHata: sim.syncHata, sync: Math.max(0, ...s.sync) * 1000,
    };
  }

  return { Sim, kos, ozetle, VARSAYILAN, ST, VITAL, ALARM };
});
