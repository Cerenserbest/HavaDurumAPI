using System.Net;
using System.Text.Json;
using HavaDurumuAPI.Models;

namespace HavaDurumuAPI.Services;

// OpenWeatherMap API'sinden hava durumu verisi çeken servis
public class HavaDurumuServisi : IHavaDurumuServisi
{
    private const string ApiAdresi = "https://api.openweathermap.org/data/2.5/weather";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public HavaDurumuServisi(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;

        // API anahtarı user-secrets üzerinden okunur, hiçbir dosyaya yazılmaz
        _apiKey = configuration["OpenWeather:ApiKey"]
            ?? throw new InvalidOperationException(
                "OpenWeather:ApiKey ayarı bulunamadı. " +
                "'dotnet user-secrets set \"OpenWeather:ApiKey\" \"<anahtarınız>\"' komutuyla ekleyin.");
    }

    public async Task<HavaDurumuDto?> GetirAsync(string sehir)
    {
        var istekAdresi = $"{ApiAdresi}?q={Uri.EscapeDataString(sehir)}&units=metric&lang=tr&appid={_apiKey}";

        var yanit = await _httpClient.GetAsync(istekAdresi);

        // OpenWeatherMap, şehir bulunamadığında 404 döner
        if (yanit.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        yanit.EnsureSuccessStatusCode();

        var icerik = await yanit.Content.ReadAsStringAsync();
        var openWeatherYaniti = JsonSerializer.Deserialize<OpenWeatherYanitModeli>(icerik);

        if (openWeatherYaniti is null)
        {
            return null;
        }

        return new HavaDurumuDto
        {
            Sehir = openWeatherYaniti.Name,
            Sicaklik = openWeatherYaniti.Main.Temp,
            HissedilenSicaklik = openWeatherYaniti.Main.FeelsLike,
            Nem = openWeatherYaniti.Main.Humidity,
            Aciklama = openWeatherYaniti.Weather.FirstOrDefault()?.Description ?? string.Empty
        };
    }
}
