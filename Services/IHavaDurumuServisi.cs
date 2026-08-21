namespace HavaDurumuAPI.Services;

// Hava durumu verisi çekme işlemlerini tanımlayan arayüz
public interface IHavaDurumuServisi
{
    // Belirtilen şehir için hava durumu bilgisini getirir; şehir bulunamazsa Veri null olur.
    // Sonuç cache'ten mi yoksa gerçek bir API çağrısından mı geldiği CacheTenGeldi ile bildirilir.
    Task<HavaDurumuSorguSonucu> GetirAsync(string sehir);
}
