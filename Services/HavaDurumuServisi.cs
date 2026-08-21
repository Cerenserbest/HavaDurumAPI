using System.Net;
using System.Text.Json;
using HavaDurumuAPI.Models;
using Microsoft.Extensions.Caching.Memory;

namespace HavaDurumuAPI.Services;

// OpenWeatherMap API'sinden hava durumu verisi çeken servis.
// Aynı şehir için sonuçlar bir süre cache'lenir, gereksiz dış API çağrısı yapılmaz.
public class HavaDurumuServisi : IHavaDurumuServisi
{
    private const string ApiAdresi = "https://api.openweathermap.org/data/2.5/weather";
    private static readonly TimeSpan CacheSuresi = TimeSpan.FromMinutes(10);

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HavaDurumuServisi> _logger;

    public HavaDurumuServisi(
        HttpClient httpClient,
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<HavaDurumuServisi> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;

        // API anahtarı user-secrets üzerinden okunur, hiçbir dosyaya yazılmaz
        _apiKey = configuration["OpenWeather:ApiKey"]
            ?? throw new InvalidOperationException(
                "OpenWeather:ApiKey ayarı bulunamadı. " +
                "'dotnet user-secrets set \"OpenWeather:ApiKey\" \"<anahtarınız>\"' komutuyla ekleyin.");
    }

    public async Task<HavaDurumuSorguSonucu> GetirAsync(string sehir)
    {
        // Cache anahtarı, "Istanbul" ve "istanbul" gibi yazımların aynı kayda düşmesi için normalize edilir
        var cacheAnahtari = $"hava:{sehir.Trim().ToLowerInvariant()}";

        if (_cache.TryGetValue(cacheAnahtari, out HavaDurumuDto? cacheliVeri) && cacheliVeri is not null)
        {
            _logger.LogInformation("{Sehir} cache'ten geldi", cacheliVeri.Sehir);
            return new HavaDurumuSorguSonucu { Veri = cacheliVeri, CacheTenGeldi = true };
        }

        var istekAdresi = $"{ApiAdresi}?q={Uri.EscapeDataString(sehir)}&units=metric&lang=tr&appid={_apiKey}";

        HttpResponseMessage yanit;
        string icerik;
        try
        {
            yanit = await _httpClient.GetAsync(istekAdresi);

            // OpenWeatherMap, şehir bulunamadığında 404 döner
            if (yanit.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("{Sehir} adlı şehir OpenWeatherMap'te bulunamadı", sehir);
                return new HavaDurumuSorguSonucu { Veri = null, CacheTenGeldi = false };
            }

            yanit.EnsureSuccessStatusCode();
            icerik = await yanit.Content.ReadAsStringAsync();
        }
        catch (Exception hata) when (hata is HttpRequestException or TaskCanceledException)
        {
            // Ağ hatası veya zaman aşımı: uygulama çökmez, çağıran taraf (controller) 503 döner.
            // Tam hata detayı burada loglanır, kullanıcıya sızdırılmaz.
            _logger.LogError(hata, "OpenWeatherMap API'sine ulaşılamadı ({Sehir})", sehir);
            throw new HavaDurumuAlinamadiException(
                $"'{sehir}' için hava durumu servisine ulaşılamadı.", hata);
        }

        var openWeatherYaniti = JsonSerializer.Deserialize<OpenWeatherYanitModeli>(icerik);

        if (openWeatherYaniti is null)
        {
            return new HavaDurumuSorguSonucu { Veri = null, CacheTenGeldi = false };
        }

        var veri = new HavaDurumuDto
        {
            Sehir = openWeatherYaniti.Name,
            Sicaklik = openWeatherYaniti.Main.Temp,
            HissedilenSicaklik = openWeatherYaniti.Main.FeelsLike,
            Nem = openWeatherYaniti.Main.Humidity,
            Aciklama = openWeatherYaniti.Weather.FirstOrDefault()?.Description ?? string.Empty
        };

        _cache.Set(cacheAnahtari, veri, CacheSuresi);
        _logger.LogInformation("{Sehir} API'den çekildi", veri.Sehir);

        return new HavaDurumuSorguSonucu { Veri = veri, CacheTenGeldi = false };
    }
}
