using HavaDurumuAPI.Models;

namespace HavaDurumuAPI.Services;

// Hava durumu verisi çekme işlemlerini tanımlayan arayüz
public interface IHavaDurumuServisi
{
    // Belirtilen şehir için hava durumu bilgisini getirir, şehir bulunamazsa null döner
    Task<HavaDurumuDto?> GetirAsync(string sehir);
}
