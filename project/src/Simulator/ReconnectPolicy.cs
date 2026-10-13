// Yeniden bağlanma politikası (deneydeki "A" faktörü):
// Bağlantı koptuktan sonra cihaz bir sonraki denemeden önce ne kadar beklesin?

public interface IReconnectPolicy
{
    // Kuralın adı ve parametreleri, deney kaydına yazılır (örn. "UstelGeriCekilme(ilk=1sn, tavan=30sn)")
    string Aciklama { get; }

    // denemeNo: bu kesintide kaçıncı bekleme (1, 2, 3, ...). Kopma sonrası ilk bekleme = 1.
    // rastgele: bu cihaza özel, tohumlu rastgele sayı üreteci (deney tekrarlanabilsin diye)
    TimeSpan SonrakiBekleme(int denemeNo, Random rastgele);
}

// En basit kural: her seferinde aynı süre bekle.
// Sorun: 100 cihaz aynı anda koptuysa, hepsi aynı saniyede geri döner.
public class SabitBekleme : IReconnectPolicy
{
    private readonly TimeSpan _sure;
    public SabitBekleme(TimeSpan sure) => _sure = sure;

    public string Aciklama => $"SabitBekleme(sure={_sure.TotalSeconds}sn)";

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

    public string Aciklama => $"UstelGeriCekilme(ilk={_ilkSn}sn, tavan={_tavanSn}sn)";

    public TimeSpan SonrakiBekleme(int denemeNo, Random rastgele)
    {
        // 1. deneme: ilk * 2^0 = ilk, 2. deneme: ilk * 2^1, 3. deneme: ilk * 2^2, ...
        double sn = _ilkSn * Math.Pow(2, denemeNo - 1);
        sn = Math.Min(sn, _tavanSn);
        return TimeSpan.FromSeconds(sn);
    }
}

// A2: Rastgeleli üstel geri çekilme. İki standarttan birleşim:
//  - RFC 6455 §7.2.3: ilk yeniden bağlanma rastgele geciktirilir, 0-5 sn "makul bir ilk gecikme".
//    Sonraki denemeler kesilmiş ikili üstel geri çekilmeyle giderek uzar.
//  - RFC 5626 §4.5: W = min(tavan, taban * 2^hata). Gerçek bekleme W'nin %50-%100'ü arasında rastgele seçilir.
// Bizim uygulama: 1. bekleme U(0, ilkAralik); sonrakiler U(0.5W, W), W = min(tavan, ilk * 2^(n-1)).
public class RastgeleliUstelGeriCekilme : IReconnectPolicy
{
    private readonly double _ilkAralikSn;  // ilk bekleme bu aralıktan rastgele seçilir (RFC 6455: 0-5 sn)
    private readonly double _ilkSn;        // üstel hesabın tabanı (saniye)
    private readonly double _tavanSn;      // W bunu geçemez (saniye)

    public RastgeleliUstelGeriCekilme(double ilkAralikSn, double ilkSn, double tavanSn)
    {
        _ilkAralikSn = ilkAralikSn;
        _ilkSn = ilkSn;
        _tavanSn = tavanSn;
    }

    public string Aciklama =>
        $"RastgeleliUstelGeriCekilme(ilkAralik=0-{_ilkAralikSn}sn, ilk={_ilkSn}sn, tavan={_tavanSn}sn, sonraki=U(0.5W,W))";

    public TimeSpan SonrakiBekleme(int denemeNo, Random rastgele)
    {
        if (denemeNo == 1)
            return TimeSpan.FromSeconds(rastgele.NextDouble() * _ilkAralikSn);

        double ust = Math.Min(_ilkSn * Math.Pow(2, denemeNo - 1), _tavanSn);   // W
        double sn = ust * (0.5 + 0.5 * rastgele.NextDouble());                 // W'nin %50-%100'ü
        return TimeSpan.FromSeconds(sn);
    }
}
