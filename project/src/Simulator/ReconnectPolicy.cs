// Yeniden bağlanma politikası (deneydeki "A" faktörü):
// Bağlantı koptuktan sonra cihaz bir sonraki denemeden önce ne kadar beklesin?

public interface IReconnectPolicy
{
    // denemeNo: bu kopmadan beri kaçıncı başarısız deneme (1, 2, 3, ...)
    // rastgele: bu cihaza özel, tohumlu rastgele sayı üreteci (deney tekrarlanabilsin diye)
    TimeSpan SonrakiBekleme(int denemeNo, Random rastgele);
}

// En basit kural: her seferinde aynı süre bekle.
// Sorun: 100 cihaz aynı anda koptuysa, hepsi aynı saniyede geri döner.
public class SabitBekleme : IReconnectPolicy
{
    private readonly TimeSpan _sure;
    public SabitBekleme(TimeSpan sure) => _sure = sure;

    public TimeSpan SonrakiBekleme(int denemeNo, Random rastgele) => _sure;
}

// A1: Üstel geri çekilme (rastgelesiz).
// Her başarısız denemede bekleme ikiye katlanır: 1, 2, 4, 8, 16, 30, 30, ...
// Rastgelelik yok, bu yüzden aynı anda kopan cihazlar yine birlikte hareket eder.
public class UstelGeriCekilme : IReconnectPolicy
{
    private readonly double _ilkSn;    // ilk bekleme (saniye)
    private readonly double _tavanSn;  // bekleme bunu geçemez (saniye)

    public UstelGeriCekilme(double ilkSn, double tavanSn)
    {
        _ilkSn = ilkSn;
        _tavanSn = tavanSn;
    }

    public TimeSpan SonrakiBekleme(int denemeNo, Random rastgele)
    {
        // 1. deneme: ilk * 2^0 = ilk, 2. deneme: ilk * 2^1, 3. deneme: ilk * 2^2, ...
        double sn = _ilkSn * Math.Pow(2, denemeNo - 1);
        sn = Math.Min(sn, _tavanSn);
        return TimeSpan.FromSeconds(sn);
    }
}
