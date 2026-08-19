namespace HavaDurumuAPI.Models;

// İstemciye dönülen hava durumu bilgisini temsil eder
public class HavaDurumuDto
{
    // Şehir adı
    public string Sehir { get; set; } = string.Empty;

    // Santigrat derece cinsinden sıcaklık
    public double Sicaklik { get; set; }

    // Hissedilen sıcaklık (santigrat derece)
    public double HissedilenSicaklik { get; set; }

    // Nem yüzdesi
    public int Nem { get; set; }

    // Hava durumu açıklaması (örn. "az bulutlu")
    public string Aciklama { get; set; } = string.Empty;
}
