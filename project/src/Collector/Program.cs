using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;

// Toplayıcı: cihazların tüm mesajlarını dinler ve her mesajın uçtan uca gecikmesini ölçer.
//   gecikme = alınma anı - mesajın PLANLANAN gönderim anı   (cihazın açık döngü zamanlamasından)
// Sınıflar: canli_vital, canli_alarm, biriken_vital, biriken_alarm
// Ayrıca "sync/ist/<id>" isteklerine "sync/yanit/<id>" ile SYNC_BYTES boyutunda cevap verir
// (cihazların "ben yokken ne değişti?" sorusunun karşılığı; sunucudan bağımsız çalışan durum servisi rolü).
// Çıktı: <OUT_PATH> (saniyelik yüzdelikler) ve <OUT_PATH>.toplam.json

Console.OutputEncoding = Encoding.UTF8;
static long MonoMs()
{
    long t = System.Diagnostics.Stopwatch.GetTimestamp();
    long f = System.Diagnostics.Stopwatch.Frequency;
    return (t / f) * 1000 + (t % f) * 1000 / f;
}
var host = Environment.GetEnvironmentVariable("BROKER_HOST") ?? "localhost";
var port = int.Parse(Environment.GetEnvironmentVariable("BROKER_PORT") ?? "1883");
var sureSn = int.Parse(Environment.GetEnvironmentVariable("DURATION_SEC") ?? "60");
var cikti = Environment.GetEnvironmentVariable("OUT_PATH") ?? "/results/collector.csv";
var syncBayt = int.Parse(Environment.GetEnvironmentVariable("SYNC_BYTES") ?? "4096");

Console.WriteLine($"toplayıcı: sunucu={host}:{port} süre={sureSn}sn sync={syncBayt}B çıktı={cikti}");
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(cikti))!);

var bitis = DateTime.UtcNow.AddSeconds(sureSn);
using var iptal = new CancellationTokenSource(TimeSpan.FromSeconds(sureSn));

var kilit = new object();
var kovalar = new Dictionary<(long sn, string sinif), List<long>>();
var gorulen = new HashSet<long>();
var benzersiz = new Dictionary<string, long>();
long tekrar = 0, syncYanit = 0, bozukMesaj = 0, negatif = 0, enNegatif = 0;
var baglantiOlaylari = new List<string>();

// İsteğe bağlı ham kayıt (RAW_PATH): her mesajın alınma/planlanan anı. Yalnızca inceleme için; büyük dosya üretir.
var hamYolu = Environment.GetEnvironmentVariable("RAW_PATH");
StreamWriter? ham = hamYolu == null ? null : new StreamWriter(hamYolu, false, new UTF8Encoding(false));
ham?.WriteLine("alinma_ms,cihaz,seq,tur,planlanan_ms,biriken,gecikme_ms");

var factory = new MqttClientFactory();
using var client = factory.CreateMqttClient();
TaskCompletionSource koptu = new(TaskCreationOptions.RunContinuationsAsynchronously);

client.DisconnectedAsync += _ =>
{
    lock (kilit) baglantiOlaylari.Add($"{DateTime.UtcNow:o} koptu");
    koptu.TrySetResult();
    return Task.CompletedTask;
};

client.ApplicationMessageReceivedAsync += e =>
{
    long simdi = MonoMs();   // tek yönlü saat: simülatörle aynı çekirdek, duvar saati sıçramalarından etkilenmez
    var konu = e.ApplicationMessage.Topic;

    if (konu.StartsWith("sync/ist/"))
    {
        // Eşitleme isteği: cevabı ayrı görevde ver (alma döngüsünü yavaşlatmasın)
        var id = konu["sync/ist/".Length..];
        _ = Task.Run(async () =>
        {
            try
            {
                await client.PublishAsync(new MqttApplicationMessageBuilder()
                    .WithTopic($"sync/yanit/{id}")
                    .WithPayload(new byte[syncBayt])
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
                    .Build(), CancellationToken.None);
                Interlocked.Increment(ref syncYanit);
            }
            catch { /* bağlantı koptuysa cihaz zaman aşımıyla anlar */ }
        });
        return Task.CompletedTask;
    }

    // Cihaz mesajı: "id|seq|A veya V|planlanan_ms|biriken(0/1)|dolgu"
    var p = Encoding.ASCII.GetString(e.ApplicationMessage.Payload).Split('|');
    if (p.Length < 5 || !long.TryParse(p[0], out var cid) || !long.TryParse(p[1], out var seq) || !long.TryParse(p[3], out var plan))
    {
        Interlocked.Increment(ref bozukMesaj);
        return Task.CompletedTask;
    }
    var sinif = (p[4] == "1" ? "biriken_" : "canli_") + (p[2] == "A" ? "alarm" : "vital");
    if (ham != null) lock (kilit) ham.WriteLine($"{simdi},{cid},{seq},{p[2]},{plan},{p[4]},{simdi - plan}");   // adli inceleme için (RAW_PATH açıkken)
    lock (kilit)
    {
        var anahtar = (simdi / 1000, sinif);
        if (!kovalar.TryGetValue(anahtar, out var liste)) kovalar[anahtar] = liste = new List<long>();
        liste.Add(simdi - plan);
        if (simdi - plan < -20) { negatif++; if (simdi - plan < enNegatif) enNegatif = simdi - plan; }   // olmaması gereken: saat anomalisi göstergesi
        if (!gorulen.Add(cid * 1_000_000 + seq)) tekrar++;
        else benzersiz[sinif] = benzersiz.GetValueOrDefault(sinif) + 1;
    }
    return Task.CompletedTask;
};

var secenekler = new MqttClientOptionsBuilder()
    .WithTcpServer(host, port)
    .WithClientId("toplayici")
    .WithProtocolVersion(MqttProtocolVersion.V500)
    .WithKeepAlivePeriod(TimeSpan.FromSeconds(10))
    .WithCleanStart(true)
    .Build();

var abonelik = new MqttClientSubscribeOptionsBuilder()
    .WithTopicFilter("cihaz/+/vital", MqttQualityOfServiceLevel.AtLeastOnce)
    .WithTopicFilter("cihaz/+/alarm", MqttQualityOfServiceLevel.AtLeastOnce)
    .WithTopicFilter("sync/ist/+", MqttQualityOfServiceLevel.AtLeastOnce)
    .Build();

// Bağlantı döngüsü: sunucu kapanıp açıldığında hızla (100 ms) geri bağlanır ve yeniden abone olur.
while (!iptal.IsCancellationRequested)
{
    try
    {
        koptu = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using var zamanAsimi = CancellationTokenSource.CreateLinkedTokenSource(iptal.Token);
        zamanAsimi.CancelAfter(TimeSpan.FromSeconds(3));
        await client.ConnectAsync(secenekler, zamanAsimi.Token);
        await client.SubscribeAsync(abonelik, zamanAsimi.Token);
        lock (kilit) baglantiOlaylari.Add($"{DateTime.UtcNow:o} baglandi");
        Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss.fff} toplayıcı bağlandı ve abone oldu");
        await koptu.Task.WaitAsync(iptal.Token);
    }
    catch (OperationCanceledException) when (iptal.IsCancellationRequested) { break; }
    catch { /* sunucu kapalı: tekrar dene */ }
    try { await Task.Delay(100, iptal.Token); } catch (OperationCanceledException) { break; }
}

// Çıktıyı yaz: saniye başına, sınıf başına adet ve yüzdelikler
static long Yuzdelik(List<long> sirali, double q) => sirali[Math.Min(sirali.Count - 1, (int)Math.Ceiling(q * sirali.Count) - 1)];
using (var yaz = new StreamWriter(cikti, false, new UTF8Encoding(false)))
{
    yaz.WriteLine("mono_s,sinif,adet,p50_ms,p95_ms,p99_ms,max_ms");   // mono_s: tek yönlü saatten saniye (açılıştan beri)
    lock (kilit)
    {
        foreach (var ((sn, sinif), liste) in kovalar.OrderBy(k => k.Key.sn).ThenBy(k => k.Key.sinif))
        {
            liste.Sort();
            yaz.WriteLine($"{sn},{sinif},{liste.Count},{Yuzdelik(liste, 0.50)},{Yuzdelik(liste, 0.95)},{Yuzdelik(liste, 0.99)},{liste[^1]}");
        }
    }
}
lock (kilit)
{
    File.WriteAllText(cikti + ".toplam.json", JsonSerializer.Serialize(new
    {
        benzersiz_alinan = benzersiz,
        tekrar_alinan = tekrar,
        sync_yanit_verilen = Interlocked.Read(ref syncYanit),
        bozuk_mesaj = Interlocked.Read(ref bozukMesaj),
        negatif_gecikmeli_mesaj_20ms_alti = negatif,
        en_negatif_gecikme_ms = enNegatif,
        baglanti_olaylari = baglantiOlaylari
    }, new JsonSerializerOptions { WriteIndented = true }));
}
ham?.Flush(); ham?.Dispose();
Console.WriteLine("toplayıcı bitti.");
