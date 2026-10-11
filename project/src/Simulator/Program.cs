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

// Hangi politika? POLICY ortam değişkeniyle seçilir: "sabit" (varsayılan) veya "ustel".
IReconnectPolicy politika = (Environment.GetEnvironmentVariable("POLICY") ?? "sabit") switch
{
    "sabit" => new SabitBekleme(TimeSpan.FromSeconds(1)),
    "ustel" => new UstelGeriCekilme(ilkSn: 1, tavanSn: 30),
    var bilinmeyen => throw new ArgumentException($"Bilinmeyen politika: {bilinmeyen}")
};

// Politika testi: cihaz yok, sunucu yok. Yalnızca "şu denemede ne kadar beklerim?" tablosu.
if (args.Contains("--politika-testi"))
{
    var rastgeleTest = new Random(1);
    Console.WriteLine($"Politika: {politika.GetType().Name}");
    for (int n = 1; n <= 10; n++)
        Console.WriteLine($"  deneme {n,2} -> bekleme {politika.SonrakiBekleme(n, rastgeleTest).TotalSeconds,5:0.##} sn");
    return;
}

Console.WriteLine($"cihaz={cihazSayisi} sunucu={host}:{port} süre={sureSn}sn tohum={tohum} politika={politika.GetType().Name}");

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(csvYolu))!);
using var csv = new StreamWriter(csvYolu, append: false);
csv.WriteLine("saniye,bagli,deneme,hata");

using var kesme = new CancellationTokenSource(TimeSpan.FromSeconds(sureSn));

// Cihazları başlat. Hepsi aynı anda bağlanmaya başlar.
var cihazlar = Enumerable.Range(1, cihazSayisi)
    .Select(id => new Device(id, host, port, politika, tohum).RunAsync(kesme.Token))
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
