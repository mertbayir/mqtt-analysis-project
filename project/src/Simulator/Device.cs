using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;

// İş yükü ayarları (hepsi girdi.json'a yazılır). VitalSn = 0 ise cihaz yalnızca bağlanır (Adım 2-4 davranışı).
public record IsYuku(int VitalSn, int AlarmSn, int MesajBayt, int SyncZamanAsimiSn, IPostConnectPolicy B, double UretimBitisSn = double.MaxValue)
{
    public bool Acik => VitalSn > 0;
}

// Bir cihazın ürettiği mesaj. PlanMs: mesajın GÖNDERİLMESİ PLANLANAN an (Unix ms), açık döngü.
// Gecikme, planlanan andan ölçülür; böylece cihaz ya da sunucu yavaşlarsa yavaş örnekler kaybolmaz.
public readonly record struct Mesaj(long Seq, bool Alarm, long PlanMs);

// Bir sanal cihaz. Yaşam döngüsü: bağlan -> (eşitle, biriken mesajları boşalt) -> bağlı kal ->
// kopunca bekle -> tekrar bağlan. Bağlıyken vital bulgu ve alarm üretir; bağlı değilken biriktirir.
public class Device
{
    // Sabit ayarlar. Deney kaydına (girdi.json) yazılır, o yüzden tek yerde tutulur.
    public const int KalpAtisiSn = 10;
    public const int BaglantiZamanAsimiSn = 5;
    public static bool TlsAcik;   // Adım 6: TLS ile bağlan (Program.cs TLS=1 ortam değişkeninden ayarlar)
    // Adım 9 (sessiz kesinti): yayın onayı (PUBACK) bu süre içinde gelmezse bağlantı ölü sayılır ve kapatılır (0 = süre sınırı yok, önceki adımlardaki davranış)
    public static int AckZamanAsimiSn;

    private readonly int _id;
    private readonly string _host;
    private readonly int _port;
    private readonly IReconnectPolicy _politika;
    private readonly Random _rastgele;
    private readonly Random _isRastgele;       // iş yükü için ayrı dizi: yeniden bağlanma dizisini bozmasın
    private readonly IsYuku _is;
    private TaskCompletionSource _koptu = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _bagli;   // 1 = şu an bağlı, 0 = değil
    private readonly double _ilkYayilimSn;   // ilk bağlanma bu süreye rastgele yayılır (başlangıç fırtınası olmasın)

    private IMqttClient? _client;
    private CancellationTokenSource? _sonrasiCts;     // bu bağlantıya ait "bağlantı sonrası" işini iptal etmek için
    private TaskCompletionSource? _syncYanit;
    private readonly ConcurrentQueue<Mesaj> _biriken = new();
    private long _seq;

    public Device(int id, string host, int port, IReconnectPolicy politika, int tohum, IsYuku isYuku, double ilkYayilimSn = 0)
    {
        _ilkYayilimSn = ilkYayilimSn;
        _id = id;
        _host = host;
        _port = port;
        _politika = politika;
        _is = isYuku;
        _rastgele = new Random(tohum * 100_000 + id);            // her cihazın kendi rastgele dizisi
        _isRastgele = new Random(tohum * 100_000 + id + 50_021); // iş yükü fazı ve B politikası için
    }

    public async Task RunAsync(CancellationToken dur)
    {
        var factory = new MqttClientFactory();
        using var client = factory.CreateMqttClient();
        _client = client;

        // Bağlantı koparsa "koptu" işaretini tamamla ve sayacı düşür.
        // Bu olay, hiç bağlanamayan denemelerde de tetiklenir. Sayacı yalnızca
        // gerçekten bağlıyken düşürürüz (_bagli bayrağı).
        client.DisconnectedAsync += _ =>
        {
            if (Interlocked.Exchange(ref _bagli, 0) == 1) Stats.BaglantiKoptu();
            _sonrasiCts?.Cancel();   // yarım kalan eşitleme/boşaltma durur, kalan mesajlar bir sonraki bağlantıda
            _koptu.TrySetResult();
            return Task.CompletedTask;
        };

        // Eşitleme cevabı: "sync/yanit/<id>" konusuna gelir.
        var syncYanitKonusu = $"sync/yanit/{_id:D5}";
        client.ApplicationMessageReceivedAsync += e =>
        {
            if (e.ApplicationMessage.Topic == syncYanitKonusu) _syncYanit?.TrySetResult();
            return Task.CompletedTask;
        };

        var optionsBuilder = new MqttClientOptionsBuilder()
            .WithTcpServer(_host, _port)
            .WithClientId($"cihaz-{_id:D5}")
            .WithProtocolVersion(MqttProtocolVersion.V500)
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(KalpAtisiSn))
            .WithTimeout(TimeSpan.FromSeconds(BaglantiZamanAsimiSn))      // bağlantı zaman aşımı (planda 5 sn)
            .WithCleanStart(true);
        // TLS seçeneği YALNIZCA TLS açıkken eklenir (kapalıyken WithTlsOptions çağrısı bile düz TCP bağlantısını bozar: günlük 12).
        if (TlsAcik)
        {
            // Yük testi: test sertifikası (kendi imzalı CA) doğrulanmaz; el sıkışma maliyeti (asimetrik işlem) aynıdır.
            optionsBuilder.WithTlsOptions(o =>
            {
                o.UseTls();
                o.WithCertificateValidationHandler(_ => true);
            });
        }
        var options = optionsBuilder.Build();

        int beklemeNo = 0;   // bu kesintide şimdiye kadar kaç kez bekledik (bağlanınca sıfırlanır)
        Task? uretici = null;

        // Deney başında herkes aynı anda bağlanmasın: ilk bağlanma [0, yayılım] sn'ye dağıtılır.
        if (_ilkYayilimSn > 0)
        {
            try { await Task.Delay(TimeSpan.FromSeconds(_rastgele.NextDouble() * _ilkYayilimSn), dur); }
            catch (OperationCanceledException) { return; }
        }

        while (!dur.IsCancellationRequested)
        {
            Stats.BaglantiDenemesi();
            try
            {
                _koptu = new(TaskCreationOptions.RunContinuationsAsynchronously);
                // Zaman aşımını kendimiz zorlarız: MQTTnet'in WithTimeout'u, karşıdan hiç cevap
                // gelmeyen (SYN'e yanıt yok) bağlantı kurulumunu sınırlamıyor. Bkz. günlük 04.
                using var zamanAsimi = CancellationTokenSource.CreateLinkedTokenSource(dur);
                zamanAsimi.CancelAfter(TimeSpan.FromSeconds(BaglantiZamanAsimiSn));
                await client.ConnectAsync(options, zamanAsimi.Token);
                _sonrasiCts = CancellationTokenSource.CreateLinkedTokenSource(dur);
                Interlocked.Exchange(ref _bagli, 1);
                Stats.BagliOldu();
                beklemeNo = 0;

                if (_is.Acik)
                {
                    // Üretici ilk başarılı bağlanmadan sonra başlar (başlangıçta biriken mesaj olmasın).
                    bool ilkBaglanti = uretici == null;
                    uretici ??= UreticiAsync(dur);
                    // Bağlantı sonrası iş (eşitle + boşalt): YALNIZCA yeniden bağlanmada. İlk açılışta "ben yokken ne
                    // değişti?" anlamsız ve başlangıç yükünü kesinti sonrası yükten ayırmak istiyoruz.
                    if (!ilkBaglanti) _ = BaglantiSonrasiAsync(_sonrasiCts.Token);
                }

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
                // Süre dolduysa "BaglantiZamanAsimi", diğerleri istisna türüyle sınıflanır
                Stats.BaglantiHatasi(ex is OperationCanceledException ? "BaglantiZamanAsimi" : ex.GetType().Name);
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

        if (uretici != null) { try { await uretici; } catch { } }
        if (client.IsConnected) await client.DisconnectAsync();
    }

    // ---- İş yükü: mesaj üretimi -------------------------------------------------------

    // Açık döngü üretici: vital bulgu her VitalSn'de, alarm her AlarmSn'de, rastgele fazla.
    // Mesajın planlanan zamanı önceden belirlidir; bağlı değilse (ya da gönderim başarısızsa) biriktirilir.
    private async Task UreticiAsync(CancellationToken dur)
    {
        long vitalMs = _is.VitalSn * 1000L;
        long alarmMs = _is.AlarmSn * 1000L;
        long simdi = Zaman.MonoMs;
        long sonrakiVital = simdi + (long)(_isRastgele.NextDouble() * vitalMs);
        long sonrakiAlarm = alarmMs > 0 ? simdi + (long)(_isRastgele.NextDouble() * alarmMs) : long.MaxValue;
        try
        {
            while (!dur.IsCancellationRequested)
            {
                bool alarm = sonrakiAlarm <= sonrakiVital;
                long plan = alarm ? sonrakiAlarm : sonrakiVital;
                // Planlanan ana kadar bekle. Zamanlayıcı erken uyanırsa (saat sıçraması vb.) tekrar bekle:
                // mesaj ASLA planlanan andan önce gönderilmez. Erken uyanma sayılır (günlük 06).
                bool ilk = true;
                long bekle;
                while ((bekle = plan - Zaman.MonoMs) > 0)
                {
                    if (!ilk) Stats.ErkenUyanma(bekle);
                    ilk = false;
                    await Task.Delay((int)Math.Min(bekle, int.MaxValue), dur);
                }

                // Koşu bitmeden kısa süre önce üretim durur: son mesajlar uçuş halindeyken iptal edilip "kayıp" görünmesin.
                if (Stats.Saat.Elapsed.TotalSeconds >= _is.UretimBitisSn) break;

                var m = new Mesaj(Interlocked.Increment(ref _seq), alarm, plan);
                if (alarm) sonrakiAlarm += alarmMs; else sonrakiVital += vitalMs;
                Stats.Uretildi(alarm);

                if (Volatile.Read(ref _bagli) == 1)
                {
                    // Canlı gönderim: planlanan anda hemen. Başarısızsa biriktirilir.
                    _ = Task.Run(async () =>
                    {
                        if (await GonderAsync(m, false, dur)) Stats.CanliGonderildi();
                        else { _biriken.Enqueue(m); Stats.BirikenEklendi(); }
                    }, dur);
                }
                else
                {
                    _biriken.Enqueue(m);
                    Stats.BirikenEklendi();
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    // Tek bir mesajı yayınlar (QoS 1). Başarılıysa true.
    private async Task<bool> GonderAsync(Mesaj m, bool biriken, CancellationToken dur)
    {
        try
        {
            var yuk = $"{_id}|{m.Seq}|{(m.Alarm ? 'A' : 'V')}|{m.PlanMs}|{(biriken ? 1 : 0)}|";
            if (yuk.Length < _is.MesajBayt) yuk = yuk.PadRight(_is.MesajBayt, 'x');
            var msg = new MqttApplicationMessageBuilder()
                .WithTopic($"cihaz/{_id:D5}/{(m.Alarm ? "alarm" : "vital")}")
                .WithPayload(Encoding.ASCII.GetBytes(yuk))
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();
            CancellationToken jeton = dur;
            CancellationTokenSource? ack = null;
            if (AckZamanAsimiSn > 0)
            {
                ack = CancellationTokenSource.CreateLinkedTokenSource(dur);
                ack.CancelAfter(TimeSpan.FromSeconds(AckZamanAsimiSn));
                jeton = ack.Token;
            }
            try { await _client!.PublishAsync(msg, jeton); }
            finally { ack?.Dispose(); }
            return true;
        }
        catch (OperationCanceledException)
        {
            if (!dur.IsCancellationRequested && AckZamanAsimiSn > 0)
            {
                // Onay gelmedi: bağlantı sessizce ölmüş olabilir. Bağlantıyı kapat, yeniden bağlanma kuralı devreye girsin.
                Stats.GonderimHatasi();
                BaglantiyiKapat();
            }
            return false;
        }
        catch (Exception ex)
        {
            Stats.GonderimHatasi();
            // MQTTnet yayın onayı beklerken kendi zaman aşımını (5 sn) hata olarak verir ama bağlantıyı kapatmaz.
            // Sessiz kesintide cihazın kopmayı fark edebilmesi için (AckZamanAsimiSn > 0) bağlantıyı biz kapatırız.
            if (AckZamanAsimiSn > 0 && ex.GetType().Name == "MqttCommunicationTimedOutException") BaglantiyiKapat();
            return false;
        }
    }

    private void BaglantiyiKapat()
    {
        _ = Task.Run(async () =>
        {
            try { using var c = new CancellationTokenSource(2000); await _client!.DisconnectAsync(new MqttClientDisconnectOptions(), c.Token); } catch { }
        });
    }

    // ---- İş yükü: bağlantı sonrası (eşitle + boşalt) --------------------------------------

    private async Task BaglantiSonrasiAsync(CancellationToken iptal)
    {
        var b = _is.B;
        try
        {
            // B3: birikmiş alarmlar ÖNCE, gecikmesiz ve peş peşe gönderilir (az sayıdadır); vital mesajlar kuyrukta kalır.
            if (b.AlarmOnce && b.BirikeniGonder)
            {
                var swA = Stopwatch.StartNew();
                var alarmlar = new List<Mesaj>();
                var digerleri = new List<Mesaj>();
                while (_biriken.TryDequeue(out var q)) (q.Alarm ? alarmlar : digerleri).Add(q);
                foreach (var d in digerleri) _biriken.Enqueue(d);   // vital mesajlar sırayla geri konur, B2 gibi sonra boşalır
                for (int i = 0; i < alarmlar.Count; i++)
                {
                    if (Volatile.Read(ref _bagli) != 1 || iptal.IsCancellationRequested || !await GonderAsync(alarmlar[i], true, iptal))
                    {
                        for (int j = i; j < alarmlar.Count; j++) _biriken.Enqueue(alarmlar[j]);   // bağlantı koptu: sonra yine denenir
                        return;
                    }
                    Stats.Bosaltildi();
                }
                Stats.Olay(_id, "alarm_flush_bitti", swA.Elapsed.TotalMilliseconds, alarmlar.Count);
            }

            var gecikme = b.BaslangicGecikmeSn(_isRastgele);
            if (gecikme > 0) await Task.Delay(TimeSpan.FromSeconds(gecikme), iptal);

            if (b.SyncYap) await SyncAsync(iptal);

            var sw = Stopwatch.StartNew();
            int adet = 0;
            if (!b.BirikeniGonder)
            {
                while (_biriken.TryDequeue(out _)) adet++;
                Stats.Atildi(adet);
                Stats.Olay(_id, "atildi", sw.Elapsed.TotalMilliseconds, adet);
                return;
            }

            while (Volatile.Read(ref _bagli) == 1 && !iptal.IsCancellationRequested && _biriken.TryDequeue(out var m))
            {
                if (!await GonderAsync(m, true, iptal)) { _biriken.Enqueue(m); return; }   // bağlantı koptu: sonra yine denenir
                Stats.Bosaltildi();
                adet++;
                if (b.MesajAraligiMs > 0) await Task.Delay(b.MesajAraligiMs, iptal);
            }
            Stats.Olay(_id, "flush_bitti", sw.Elapsed.TotalMilliseconds, adet);
        }
        catch (OperationCanceledException) { }
    }

    // MQTT 5 istek/cevap benzeri eşitleme: "sync/ist/<id>" konusuna istek, "sync/yanit/<id>" konusundan cevap.
    private async Task SyncAsync(CancellationToken iptal)
    {
        var sw = Stopwatch.StartNew();
        _syncYanit = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            await _client!.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter($"sync/yanit/{_id:D5}").Build(), iptal);
            var istek = new MqttApplicationMessageBuilder()
                .WithTopic($"sync/ist/{_id:D5}")
                .WithPayload(Encoding.ASCII.GetBytes(_id.ToString()))
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();
            await _client.PublishAsync(istek, iptal);

            using var zamanAsimi = CancellationTokenSource.CreateLinkedTokenSource(iptal);
            zamanAsimi.CancelAfter(TimeSpan.FromSeconds(_is.SyncZamanAsimiSn));
            await _syncYanit.Task.WaitAsync(zamanAsimi.Token);
            Stats.Olay(_id, "sync_ok", sw.Elapsed.TotalMilliseconds, 1);
        }
        catch (OperationCanceledException) when (iptal.IsCancellationRequested) { throw; }
        catch (Exception)
        {
            Stats.Olay(_id, "sync_hata", sw.Elapsed.TotalMilliseconds, 0);
        }
    }
}
