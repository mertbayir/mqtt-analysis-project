using System.Text;
using MQTTnet;
using MQTTnet.Protocol;

// Adım 1: Tek bir sanal cihaz sunucuya bağlanır, bir mesaj yollar, aynı mesajı geri alır.

// Sunucu adresi: Windows'tan çalışırken "localhost", Docker içinde "broker" olacak.
var host = Environment.GetEnvironmentVariable("BROKER_HOST") ?? "localhost";
var port = int.Parse(Environment.GetEnvironmentVariable("BROKER_PORT") ?? "1883");

var factory = new MqttClientFactory();
using var client = factory.CreateMqttClient();

// Sunucudan mesaj gelince bu fonksiyon çalışır.
client.ApplicationMessageReceivedAsync += e =>
{
    var konu = e.ApplicationMessage.Topic;
    var metin = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
    Console.WriteLine($"[ALINDI] konu={konu} mesaj={metin}");
    return Task.CompletedTask;
};

var options = new MqttClientOptionsBuilder()
    .WithTcpServer(host, port)
    .WithClientId("cihaz-0001")
    .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)  // MQTT 5
    .WithKeepAlivePeriod(TimeSpan.FromSeconds(10))
    .WithCleanStart(true)
    .Build();

Console.WriteLine($"Bağlanılıyor: {host}:{port}");
var sonuc = await client.ConnectAsync(options);
Console.WriteLine($"Bağlandı. Sonuç kodu: {sonuc.ResultCode}");

// Kendi konumuza abone ol, sonra o konuya mesaj yolla.
var konuAdi = "cihaz/0001/vital";
await client.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
    .WithTopicFilter(konuAdi)
    .Build());

await client.PublishStringAsync(konuAdi, "nabiz=72", MqttQualityOfServiceLevel.AtLeastOnce);

await Task.Delay(500);   // mesajın dönmesi için kısa bekleme
await client.DisconnectAsync();
Console.WriteLine("Bağlantı kapatıldı.");
