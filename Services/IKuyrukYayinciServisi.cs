using HavaDurumuOrtak.Models;

namespace HavaDurumuAPI.Services;

// RabbitMQ kuyruğuna sorgu kaydı mesajı gönderen servisin arayüzü
public interface IKuyrukYayinciServisi
{
    // Verilen sorgu kaydını kuyruğa JSON mesaj olarak gönderir
    Task YayinlaAsync(SorguKaydi sorguKaydi);
}
