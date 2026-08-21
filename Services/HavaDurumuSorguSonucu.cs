using HavaDurumuAPI.Models;

namespace HavaDurumuAPI.Services;

// HavaDurumuServisi'nin sorgu sonucunu, verinin cache'ten mi yoksa
// gerçek bir API çağrısından mı geldiğini belirterek taşır
public class HavaDurumuSorguSonucu
{
    // Şehir bulunamazsa null olur
    public HavaDurumuDto? Veri { get; init; }

    // true ise veri cache'ten geldi, false ise gerçek bir OpenWeatherMap çağrısından geldi
    public bool CacheTenGeldi { get; init; }
}
