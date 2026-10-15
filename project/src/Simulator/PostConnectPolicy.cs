// Bağlantı sonrası politika (deneydeki "B" faktörü):
// Cihaz yeniden bağlandıktan sonra ne yapsın? "Eşitle" (durum bilgisini iste) ve
// "boşalt" (kesinti sırasında biriken mesajları gönder).

public interface IPostConnectPolicy
{
    string Aciklama { get; }
    bool SyncYap { get; }                    // bağlanınca durum bilgisi iste mi?
    bool BirikeniGonder { get; }             // biriken mesajlar sunucuya gönderilsin mi (false: atılır)?
    double BaslangicGecikmeSn(Random r);     // bağlandıktan sonra eşitleme/boşaltmadan önce bekleme
    int MesajAraligiMs { get; }              // biriken mesajlar arasında bekleme (0 = peş peşe, en hızlı)
}

// B0 (referans): bağlantı sonrası hiçbir iş yok. Biriken mesajlar atılır, eşitleme yapılmaz.
// Yalnızca "bağlanma" aşamasının etkisini görmek için.
public class B0Referans : IPostConnectPolicy
{
    public string Aciklama => "B0Referans(sync=yok, biriken=atilir)";
    public bool SyncYap => false;
    public bool BirikeniGonder => false;
    public double BaslangicGecikmeSn(Random r) => 0;
    public int MesajAraligiMs => 0;
}

// B1 (hemen): bağlanır bağlanmaz eşitle, sonra biriken tüm mesajları peş peşe boşalt.
public class B1Hemen : IPostConnectPolicy
{
    public string Aciklama => "B1Hemen(gecikme=0, aralik=0ms)";
    public bool SyncYap => true;
    public bool BirikeniGonder => true;
    public double BaslangicGecikmeSn(Random r) => 0;
    public int MesajAraligiMs => 0;
}

// B2 (ayrı zamanlı): bağlandıktan sonra [0, pencere] sn rastgele bekle, sonra eşitle ve
// biriken mesajları aralıklı (örn. 200 ms) boşalt. Canlı mesajlara yol bırakır.
public class B2Ayrik : IPostConnectPolicy
{
    private readonly double _pencereSn;
    private readonly int _aralikMs;
    public B2Ayrik(double pencereSn, int aralikMs) { _pencereSn = pencereSn; _aralikMs = aralikMs; }

    public string Aciklama => $"B2Ayrik(gecikme=U(0,{_pencereSn})sn, aralik={_aralikMs}ms)";
    public bool SyncYap => true;
    public bool BirikeniGonder => true;
    public double BaslangicGecikmeSn(Random r) => r.NextDouble() * _pencereSn;
    public int MesajAraligiMs => _aralikMs;
}
