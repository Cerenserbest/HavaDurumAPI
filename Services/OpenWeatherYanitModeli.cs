using System.Text.Json.Serialization;

namespace HavaDurumuAPI.Services;

// OpenWeatherMap API'sinden dönen JSON yanıtının ihtiyaç duyulan alanlarını temsil eder.
// Sadece bu servisin içinde kullanılan bir eşleme modelidir, dışarıya açılmaz.
internal class OpenWeatherYanitModeli
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("main")]
    public OpenWeatherAnaVeriler Main { get; set; } = new();

    [JsonPropertyName("weather")]
    public List<OpenWeatherAciklama> Weather { get; set; } = new();
}

internal class OpenWeatherAnaVeriler
{
    [JsonPropertyName("temp")]
    public double Temp { get; set; }

    [JsonPropertyName("feels_like")]
    public double FeelsLike { get; set; }

    [JsonPropertyName("humidity")]
    public int Humidity { get; set; }
}

internal class OpenWeatherAciklama
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}
