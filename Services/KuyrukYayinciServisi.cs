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

    public KuyrukYayinciServisi(IConfiguration configuration)
    {
        _hostName = configuration["RabbitMq:HostName"] ?? "localhost";
        _userName = configuration["RabbitMq:UserName"] ?? "guest";
        _password = configuration["RabbitMq:Password"] ?? "guest";
        _kuyrukAdi = configuration["RabbitMq:KuyrukAdi"] ?? "havadurumu-kuyruk";
    }

    public async Task YayinlaAsync(SorguKaydi sorguKaydi)
    {
        var factory = new ConnectionFactory
        {
            HostName = _hostName,
            UserName = _userName,
            Password = _password
        };

        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        // Kuyruk yoksa oluşturulur, varsa dokunulmaz (consumer tarafındaki tanımla aynı olmalı)
        await channel.QueueDeclareAsync(queue: _kuyrukAdi, durable: false, exclusive: false, autoDelete: false);

        var mesajJson = JsonSerializer.Serialize(sorguKaydi);
        var govde = Encoding.UTF8.GetBytes(mesajJson);

        await channel.BasicPublishAsync(exchange: string.Empty, routingKey: _kuyrukAdi, body: govde);
    }
}
