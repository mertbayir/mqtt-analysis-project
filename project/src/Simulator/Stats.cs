using System.Collections.Concurrent;
using System.Diagnostics;

// Tek yönlü (monotonic) saat: açılıştan beri geçen ms. Duvar saati (UtcNow) WSL2'de ani sıçrayabildiği için
// (günlük 06, Bulgu 1) gecikmeler bu saatle ölçülür. Linux'ta CLOCK_MONOTONIC; aynı çekirdeği paylaşan
// konteynerler (simülatör, toplayıcı) arasında karşılaştırılabilir.
public static class Zaman
{
    public static long MonoMs
    {
        get
        {
            long t = Stopwatch.GetTimestamp();
            return (t / Stopwatch.Frequency) * 1000 + (t % Stopwatch.Frequency) * 1000 / Stopwatch.Frequency;
        }
    }
}

// Tüm cihazların ortak sayaçları. Binlerce görev aynı anda artırdığı için
// Interlocked kullanıyoruz: iki görev aynı anda "+1" yaparsa biri kaybolmasın.
public static class Stats
{
    private static int _bagli;
    private static int _deneme;
    private static int _hata;
    private static readonly ConcurrentDictionary<string, int> _hataTurleri = new();

    // Koşu saati: olay zamanlarını "simülatör başlangıcından beri geçen sn" olarak yazarız.
    public static readonly Stopwatch Saat = Stopwatch.StartNew();

    // Trafik sayaçları (iş yükü açıksa)
    private static long _uretilenVital, _uretilenAlarm, _canliGonderilen, _birikenEklenen, _atilan, _bosaltilan, _gonderimHatasi;
    private static int _syncTamam, _syncHata;

    // Olay günlüğü: eşitleme ve boşaltma bitişleri (cihaz, tür, süre, adet)
    private static readonly ConcurrentQueue<string> _olaylar = new();

    public static int Bagli => Volatile.Read(ref _bagli);

    public static void BaglantiDenemesi() => Interlocked.Increment(ref _deneme);
    public static void BagliOldu() => Interlocked.Increment(ref _bagli);
    public static void BaglantiKoptu() => Interlocked.Decrement(ref _bagli);

    public static void BaglantiHatasi(string tur)
    {
        Interlocked.Increment(ref _hata);
        _hataTurleri.AddOrUpdate(tur, 1, (_, eski) => eski + 1);
    }

    // Son çağrıdan beri kaç deneme ve hata oldu? (sayaçları sıfırlar)
    public static (int deneme, int hata) SaniyeLik() =>
        (Interlocked.Exchange(ref _deneme, 0), Interlocked.Exchange(ref _hata, 0));

    public static IReadOnlyDictionary<string, int> HataTurleri => _hataTurleri;

    public static void Uretildi(bool alarm)
    {
        if (alarm) Interlocked.Increment(ref _uretilenAlarm); else Interlocked.Increment(ref _uretilenVital);
    }
    public static void CanliGonderildi() => Interlocked.Increment(ref _canliGonderilen);
    public static void BirikenEklendi() => Interlocked.Increment(ref _birikenEklenen);
    public static void Atildi(int adet) => Interlocked.Add(ref _atilan, adet);
    public static void Bosaltildi() => Interlocked.Increment(ref _bosaltilan);
    public static void GonderimHatasi() => Interlocked.Increment(ref _gonderimHatasi);

    private static long _erkenUyanma, _enBuyukErkenMs;
    public static void ErkenUyanma(long erkenMs)
    {
        Interlocked.Increment(ref _erkenUyanma);
        long eski;
        while (erkenMs > (eski = Interlocked.Read(ref _enBuyukErkenMs)) && Interlocked.CompareExchange(ref _enBuyukErkenMs, erkenMs, eski) != eski) { }
    }

    public static void Olay(int cihaz, string tur, double sureMs, int adet)
    {
        if (tur == "sync_ok") Interlocked.Increment(ref _syncTamam);
        if (tur == "sync_hata") Interlocked.Increment(ref _syncHata);
        _olaylar.Enqueue($"{Saat.Elapsed.TotalSeconds:0.000},{cihaz},{tur},{sureMs:0.0},{adet}");
    }

    public static IEnumerable<string> Olaylar => _olaylar;

    public static object Toplamlar() => new
    {
        uretilen_vital = Interlocked.Read(ref _uretilenVital),
        uretilen_alarm = Interlocked.Read(ref _uretilenAlarm),
        canli_gonderilen = Interlocked.Read(ref _canliGonderilen),
        biriken_eklenen = Interlocked.Read(ref _birikenEklenen),
        atilan_biriken = Interlocked.Read(ref _atilan),
        bosaltilan = Interlocked.Read(ref _bosaltilan),
        gonderim_hatasi = Interlocked.Read(ref _gonderimHatasi),
        erken_uyanma_sayisi = Interlocked.Read(ref _erkenUyanma),
        en_buyuk_erken_uyanma_ms = Interlocked.Read(ref _enBuyukErkenMs),
        sync_tamam = Volatile.Read(ref _syncTamam),
        sync_hata = Volatile.Read(ref _syncHata),
    };
}
