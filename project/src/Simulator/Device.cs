using MQTTnet;
using MQTTnet.Formatter;

// Bir sanal cihaz. Yaşam döngüsü: bağlan -> bağlı kal -> kopunca beklemeden sonra tekrar bağlan.
public class Device
{
    private readonly int _id;
    private readonly string _host;
    private readonly int _port;
    private readonly IReconnectPolicy _politika;
    private readonly Random _rastgele;
    private TaskCompletionSource _koptu = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _bagli;   // 1 = şu an bağlı, 0 = değil

    public Device(int id, string host, int port, IReconnectPolicy politika, int tohum)
    {
        _id = id;
        _host = host;
        _port = port;
        _politika = politika;
        _rastgele = new Random(tohum * 100_000 + id);   // her cihazın kendi rastgele dizisi
    }

    public async Task RunAsync(CancellationToken dur)
    {
        var factory = new MqttClientFactory();
        using var client = factory.CreateMqttClient();

        // Bağlantı koparsa "koptu" işaretini tamamla ve sayacı düşür.
        // Bu olay, hiç bağlanamayan denemelerde de tetiklenir. Sayacı yalnızca
        // gerçekten bağlıyken düşürürüz (_bagli bayrağı).
        client.DisconnectedAsync += _ =>
        {
            if (Interlocked.Exchange(ref _bagli, 0) == 1) Stats.BaglantiKoptu();
            _koptu.TrySetResult();
            return Task.CompletedTask;
        };

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_host, _port)
            .WithClientId($"cihaz-{_id:D5}")
            .WithProtocolVersion(MqttProtocolVersion.V500)
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(10))
            .WithTimeout(TimeSpan.FromSeconds(5))      // bağlantı zaman aşımı (planda 5 sn)
            .WithCleanStart(true)
            .Build();

        int beklemeNo = 0;   // bu kesintide şimdiye kadar kaç kez bekledik (bağlanınca sıfırlanır)

        while (!dur.IsCancellationRequested)
        {
            Stats.BaglantiDenemesi();
            try
            {
                _koptu = new(TaskCreationOptions.RunContinuationsAsynchronously);
                await client.ConnectAsync(options, dur);
                Interlocked.Exchange(ref _bagli, 1);
                Stats.BagliOldu();
                beklemeNo = 0;

                // Kopana kadar burada bekle (hiçbir işlemci harcamadan).
                await _koptu.Task.WaitAsync(dur);
            }
            catch (OperationCanceledException) when (dur.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Bağlanılamadı (sunucu kapalı, reddedildi, zaman aşımı...)
                Stats.BaglantiHatasi(ex.GetType().Name);
            }

            try
            {
                // Her bekleme bir öncekinden sonra gelir: 1., 2., 3. ... (kopma sonrası ilk bekleme = 1)
                beklemeNo++;
                var bekleme = _politika.SonrakiBekleme(beklemeNo, _rastgele);
                await Task.Delay(bekleme, dur);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        if (client.IsConnected) await client.DisconnectAsync();
    }
}
