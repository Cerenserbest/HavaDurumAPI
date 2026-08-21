using System.Text;
using System.Text.Json;
using HavaDurumuOrtak.Models;
using RabbitMQ.Client;

namespace HavaDurumuAPI.Services;

// SorguKaydi mesajlarını RabbitMQ'daki "havadurumu-kuyruk" kuyruğuna gönderen servis
public class KuyrukYayinciServisi : IKuyrukYayinciServisi
{
    private readonly string _hostName;
    private readonly string _userName;
    private readonly string _password;
    private readonly string _kuyrukAdi;
    private readonly ILogger<KuyrukYayinciServisi> _logger;

    public KuyrukYayinciServisi(IConfiguration configuration, ILogger<KuyrukYayinciServisi> logger)
    {
        _hostName = configuration["RabbitMq:HostName"] ?? "localhost";
        _userName = configuration["RabbitMq:UserName"] ?? "guest";
        _password = configuration["RabbitMq:Password"] ?? "guest";
        _kuyrukAdi = configuration["RabbitMq:KuyrukAdi"] ?? "havadurumu-kuyruk";
        _logger = logger;
    }

    public async Task YayinlaAsync(SorguKaydi sorguKaydi)
    {
        var factory = new ConnectionFactory
        {
            HostName = _hostName,
            UserName = _userName,
            Password = _password
        };

        try
        {
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            // Kuyruk yoksa oluşturulur, varsa dokunulmaz (consumer tarafındaki tanımla aynı olmalı)
            await channel.QueueDeclareAsync(queue: _kuyrukAdi, durable: false, exclusive: false, autoDelete: false);

            var mesajJson = JsonSerializer.Serialize(sorguKaydi);
            var govde = Encoding.UTF8.GetBytes(mesajJson);

            await channel.BasicPublishAsync(exchange: string.Empty, routingKey: _kuyrukAdi, body: govde);

            _logger.LogInformation("{Sehir} sorgu kaydı '{Kuyruk}' kuyruğuna gönderildi", sorguKaydi.Sehir, _kuyrukAdi);
        }
        catch (Exception hata)
        {
            // Tam hata detayı burada loglanır; istisna global hata yakalayıcıya bırakılır (500 döner)
            _logger.LogError(hata, "{Sehir} sorgu kaydı kuyruğa gönderilemedi", sorguKaydi.Sehir);
            throw;
        }
    }
}
