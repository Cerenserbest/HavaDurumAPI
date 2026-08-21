namespace HavaDurumuAPI.Services;

// OpenWeatherMap API'sine ulaşılamadığında (ağ hatası, zaman aşımı vb.) fırlatılır.
// Controller bu istisnayı yakalayıp kullanıcıya 503 döner.
public class HavaDurumuAlinamadiException : Exception
{
    public HavaDurumuAlinamadiException(string mesaj, Exception icHata) : base(mesaj, icHata)
    {
    }
}
