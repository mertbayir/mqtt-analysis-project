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
