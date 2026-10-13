using System.Diagnostics;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;   // dosyaya yönlendirilince Türkçe karakter bozulmasın

// Adım 2: N sanal cihaz bağlanır. Sunucu dışarıdan kapatılıp açılır.
// Her saniye kaç cihaz bağlı, kaç deneme ve kaç hata olduğunu yazarız.

var host = Environment.GetEnvironmentVariable("BROKER_HOST") ?? "localhost";
var port = int.Parse(Environment.GetEnvironmentVariable("BROKER_PORT") ?? "1883");
var cihazSayisi = int.Parse(Environment.GetEnvironmentVariable("DEVICES") ?? "100");
var sureSn = int.Parse(Environment.GetEnvironmentVariable("DURATION_SEC") ?? "75");
var tohum = int.Parse(Environment.GetEnvironmentVariable("SEED") ?? "1");
var csvYolu = Environment.GetEnvironmentVariable("CSV_PATH") ?? "results/adim2.csv";
var yayilimSn = double.Parse(Environment.GetEnvironmentVariable("START_SPREAD_SEC") ?? "0",
    System.Globalization.CultureInfo.InvariantCulture);

// Hangi politika? POLICY ortam değişkeniyle seçilir: "sabit" (varsayılan) veya "ustel".
IReconnectPolicy politika = (Environment.GetEnvironmentVariable("POLICY") ?? "sabit") switch
{
    "sabit" => new SabitBekleme(TimeSpan.FromSeconds(1)),
    "ustel" => new UstelGeriCekilme(ilkSn: 1, tavanSn: 30),
    "ustel-rastgele" => new RastgeleliUstelGeriCekilme(ilkAralikSn: 5, ilkSn: 1, tavanSn: 30),
    var bilinmeyen => throw new ArgumentException($"Bilinmeyen politika: {bilinmeyen}")
};

// Politika testi: cihaz yok, sunucu yok. "Şu bekleme sırasında ne kadar beklerim?" tablosu.
// 3 farklı cihaz yan yana gösterilir: rastgeleli kuralda her cihazın farklı çıkması gerekir.
if (args.Contains("--politika-testi"))
{
    var cihazlar3 = new[] { 1, 2, 3 }.Select(id => new Random(tohum * 100_000 + id)).ToArray();
    Console.WriteLine($"Politika: {politika.GetType().Name}  (3 cihaz, tohum={tohum})");
    Console.WriteLine("  bekleme no |  cihaz 1 |  cihaz 2 |  cihaz 3   (saniye)");
    for (int n = 1; n <= 8; n++)
    {
        var sureler = cihazlar3.Select(r => politika.SonrakiBekleme(n, r).TotalSeconds);
        Console.WriteLine($"  {n,10} | " + string.Join(" | ", sureler.Select(s => $"{s,8:0.00}")));
    }
    return;
}

Console.WriteLine($"cihaz={cihazSayisi} sunucu={host}:{port} süre={sureSn}sn tohum={tohum} politika={politika.Aciklama}");

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(csvYolu))!);

// Deney kaydı: bu koşunun TÜM girdileri, sonuç CSV'sinin yanına yazılır.
var girdiYolu = Path.ChangeExtension(csvYolu, ".girdi.json");
var girdi = new
{
    baslangic_utc = DateTime.UtcNow.ToString("o"),
    sunucu = $"{host}:{port}",
    cihaz_sayisi = cihazSayisi,
    sure_sn = sureSn,
    tohum,
    politika = politika.Aciklama,
    mqtt_surumu = "5.0",
    kalp_atisi_sn = Device.KalpAtisiSn,
    baglanti_zaman_asimi_sn = Device.BaglantiZamanAsimiSn,
    temiz_baslangic = true,
    ilk_baglanma_yayilimi_sn = yayilimSn,
    kutuphane_otomatik_yeniden_baglanma = false,
    dotnet = Environment.Version.ToString(),
    ozet_aralik_sn = 1
};
File.WriteAllText(girdiYolu, System.Text.Json.JsonSerializer.Serialize(girdi,
    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
using var csv = new StreamWriter(csvYolu, append: false);
csv.WriteLine("saniye,bagli,deneme,hata");

using var kesme = new CancellationTokenSource(TimeSpan.FromSeconds(sureSn));

// Cihazları başlat. Hepsi aynı anda bağlanmaya başlar.
var cihazlar = Enumerable.Range(1, cihazSayisi)
    .Select(id => new Device(id, host, port, politika, tohum, yayilimSn).RunAsync(kesme.Token))
    .ToList();

// Her saniye bir özet satırı.
var saat = Stopwatch.StartNew();
int saniye = 0;
while (!kesme.IsCancellationRequested)
{
    try { await Task.Delay(1000, kesme.Token); } catch (OperationCanceledException) { break; }
    saniye++;
    var (deneme, hata) = Stats.SaniyeLik();
    var satir = $"{saniye},{Stats.Bagli},{deneme},{hata}";
    csv.WriteLine(satir);
    csv.Flush();
    Console.WriteLine($"t={saniye,3}sn  bağlı={Stats.Bagli,4}  deneme/sn={deneme,4}  hata/sn={hata,4}");
}

await Task.WhenAll(cihazlar);
Console.WriteLine("Hata türleri:");
foreach (var (tur, sayi) in Stats.HataTurleri.OrderByDescending(x => x.Value))
    Console.WriteLine($"  {tur}: {sayi}");
Console.WriteLine("Bitti.");
