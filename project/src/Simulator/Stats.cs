using System.Collections.Concurrent;

// Tüm cihazların ortak sayaçları. Binlerce görev aynı anda artırdığı için
// Interlocked kullanıyoruz: iki görev aynı anda "+1" yaparsa biri kaybolmasın.
public static class Stats
{
    private static int _bagli;
    private static int _deneme;
    private static int _hata;
    private static readonly ConcurrentDictionary<string, int> _hataTurleri = new();

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
}
