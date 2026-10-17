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
Device.TlsAcik = Environment.GetEnvironmentVariable("TLS") == "1";   // Adım 6: cihazlar TLS ile bağlanır (port 8883)
Device.AckZamanAsimiSn = int.Parse(Environment.GetEnvironmentVariable("ACK_TIMEOUT_SEC") ?? "0");   // Adım 9: yayın onayı zaman aşımı (0 = yok)
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

// İş yükü (Adım 5): VITAL_SEC = 0 ise cihazlar yalnızca bağlanır. Açıksa vital + alarm üretilir,
// kesintide biriktirilir ve bağlantı sonrası politikaya (B) göre eşitlenir/boşaltılır.
int Ortam(string ad, int varsayilan) => int.Parse(Environment.GetEnvironmentVariable(ad) ?? varsayilan.ToString());
IPostConnectPolicy bPolitika = (Environment.GetEnvironmentVariable("POLICY_B") ?? "b0") switch
{
    "b0" => new B0Referans(),
    "b1" => new B1Hemen(),
    "b2" => new B2Ayrik(pencereSn: Ortam("B2_WINDOW_SEC", 30), aralikMs: Ortam("B2_INTERVAL_MS", 200)),
    "b3" => new B3AlarmOnce(pencereSn: Ortam("B2_WINDOW_SEC", 30), aralikMs: Ortam("B2_INTERVAL_MS", 200)),
    var bilinmeyen => throw new ArgumentException($"Bilinmeyen B politikası: {bilinmeyen}")
};
var isYuku = new IsYuku(VitalSn: Ortam("VITAL_SEC", 0), AlarmSn: Ortam("ALARM_SEC", 10),
    MesajBayt: Ortam("MSG_BYTES", 128), SyncZamanAsimiSn: Ortam("SYNC_TIMEOUT_SEC", 10), B: bPolitika,
    UretimBitisSn: sureSn - Ortam("PRODUCE_STOP_BEFORE_END_SEC", 5));

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
    baslangic_mono_ms = Zaman.MonoMs,   // gecikme ve zaman ekseni hizalaması bu saatle (duvar saati sıçrayabilir)
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
    is_yuku_acik = isYuku.Acik,
    vital_aralik_sn = isYuku.VitalSn,
    alarm_aralik_sn = isYuku.AlarmSn,
    mesaj_bayt = isYuku.MesajBayt,
    sync_zaman_asimi_sn = isYuku.SyncZamanAsimiSn,
    baglanti_sonrasi_politika = isYuku.B.Aciklama,
    uretim_bitis_sn = isYuku.UretimBitisSn,
    tls = Device.TlsAcik,
    ack_zaman_asimi_sn = Device.AckZamanAsimiSn,
    kutuphane_otomatik_yeniden_baglanma = false,
    dotnet = Environment.Version.ToString(),
    ozet_aralik_sn = 1
};
File.WriteAllText(girdiYolu, System.Text.Json.JsonSerializer.Serialize(girdi,
    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
using var csv = new StreamWriter(csvYolu, append: false);
csv.WriteLine("saniye,bagli,deneme,hata");

using var kesme = new CancellationTokenSource(TimeSpan.FromSeconds(sureSn));

Stats.Saat.Restart();   // olay zamanları ve üretim bitişi "cihazlar başladığı andan" ölçülür

// Cihazları başlat. Hepsi aynı anda bağlanmaya başlar.
var cihazlar = Enumerable.Range(1, cihazSayisi)
    .Select(id => new Device(id, host, port, politika, tohum, isYuku, yayilimSn).RunAsync(kesme.Token))
    .ToList();

// Saat bekçisi: duvar saati (UtcNow) ile tek yönlü sayaç (Stopwatch) arasındaki fark ani değişirse,
// makinenin (WSL2 sanal makinesi) saati sıçramıştır. Gecikme ölçümünü bozabileceği için olay olarak kaydedilir.
if (isYuku.Acik)
{
    _ = Task.Run(async () =>
    {
        var sw = Stopwatch.StartNew();
        double taban = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - sw.Elapsed.TotalMilliseconds;
        while (!kesme.IsCancellationRequested)
        {
            try { await Task.Delay(100, kesme.Token); } catch (OperationCanceledException) { break; }
            double ofset = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - sw.Elapsed.TotalMilliseconds;
            if (Math.Abs(ofset - taban) > 30)
            {
                Stats.Olay(0, "saat_sicramasi", ofset - taban, 0);   // sure_ms sütunu: sıçrama miktarı (ms), + ileri / - geri
                taban = ofset;
            }
        }
    });
}

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

// Olay günlüğü (eşitleme/boşaltma bitişleri) ve toplamlar
if (isYuku.Acik)
{
    File.WriteAllLines(Path.ChangeExtension(csvYolu, ".olay.csv"),
        new[] { "t_sn,cihaz,tur,sure_ms,adet" }.Concat(Stats.Olaylar));
    File.WriteAllText(Path.ChangeExtension(csvYolu, ".toplam.json"),
        System.Text.Json.JsonSerializer.Serialize(Stats.Toplamlar(), new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine("Toplamlar: " + System.Text.Json.JsonSerializer.Serialize(Stats.Toplamlar()));
}

Console.WriteLine("Hata türleri:");
foreach (var (tur, sayi) in Stats.HataTurleri.OrderByDescending(x => x.Value))
    Console.WriteLine($"  {tur}: {sayi}");
Console.WriteLine("Bitti.");
