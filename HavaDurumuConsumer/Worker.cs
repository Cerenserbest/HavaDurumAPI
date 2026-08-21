using System.Text;
using System.Text.Json;
using HavaDurumuOrtak.Data;
using HavaDurumuOrtak.Models;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HavaDurumuConsumer;

// "havadurumu-kuyruk" kuyruğunu dinleyip gelen sorgu kayıtlarını veritabanına yazan arka plan servisi
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;

    public Worker(ILogger<Worker> logger, IConfiguration configuration, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var kuyrukAdi = _configuration["RabbitMq:KuyrukAdi"] ?? "havadurumu-kuyruk";

        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMq:HostName"] ?? "localhost",
            UserName = _configuration["RabbitMq:UserName"] ?? "guest",
            Password = _configuration["RabbitMq:Password"] ?? "guest"
        };

        using var connection = await BaglantiKurAsync(factory, stoppingToken);
        using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // Kuyruk yoksa oluşturulur, varsa dokunulmaz (API tarafındaki tanımla aynı olmalı)
        await channel.QueueDeclareAsync(
            queue: kuyrukAdi,
            durable: false,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (model, eventArgs) =>
        {
            try
            {
                var mesajJson = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                var sorguKaydi = JsonSerializer.Deserialize<SorguKaydi>(mesajJson);

                if (sorguKaydi is not null)
                {
                    // DbContext scoped olduğu için her mesaj için ayrı bir scope açılır
                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<UygulamaDbContext>();

                    // Id, mesajda gelmiş olsa bile veritabanının kendi ürettiği değeri kullanır
                    sorguKaydi.Id = 0;

                    dbContext.SorguKayitlari.Add(sorguKaydi);
                    await dbContext.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation("{Sehir} kaydedildi", sorguKaydi.Sehir);
                }

                await channel.BasicAckAsync(eventArgs.DeliveryTag, false, stoppingToken);
            }
            catch (Exception hata)
            {
                _logger.LogError(hata, "Mesaj işlenirken hata oluştu");
                await channel.BasicNackAsync(eventArgs.DeliveryTag, false, requeue: true, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(
            queue: kuyrukAdi,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("'{Kuyruk}' kuyruğu dinleniyor", kuyrukAdi);

        // Uygulama durdurulana kadar servis canlı kalır
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    // RabbitMQ, konteyner ayağa kalkarken "healthy" görünse bile AMQP portu bir süre
    // hazır olmayabilir; bu yüzden bağlantı birkaç kez tekrar denenir
    private async Task<IConnection> BaglantiKurAsync(ConnectionFactory factory, CancellationToken stoppingToken)
    {
        const int maksimumDeneme = 10;
        var bekleme = TimeSpan.FromSeconds(3);

        for (var deneme = 1; deneme <= maksimumDeneme; deneme++)
        {
            try
            {
                return await factory.CreateConnectionAsync(stoppingToken);
            }
            catch (Exception hata) when (deneme < maksimumDeneme)
            {
                _logger.LogWarning(
                    "RabbitMQ'ya bağlanılamadı ({Deneme}/{Maksimum}), {Saniye} saniye sonra tekrar denenecek: {Mesaj}",
                    deneme, maksimumDeneme, bekleme.TotalSeconds, hata.Message);
                await Task.Delay(bekleme, stoppingToken);
            }
        }

        // Son deneme de başarısız olursa istisna doğal şekilde fırlatılır
        return await factory.CreateConnectionAsync(stoppingToken);
    }
}
