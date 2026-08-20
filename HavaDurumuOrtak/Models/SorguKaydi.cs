namespace HavaDurumuOrtak.Models;

// Yapılan her hava durumu sorgusunun veritabanına kaydedilen hali
public class SorguKaydi
{
    public int Id { get; set; }

    // Sorgulanan şehir adı
    public string Sehir { get; set; } = string.Empty;

    // Santigrat derece cinsinden sıcaklık
    public double Sicaklik { get; set; }

    // Hissedilen sıcaklık (santigrat derece)
    public double HissedilenSicaklik { get; set; }

    // Nem yüzdesi
    public int Nem { get; set; }

    // Hava durumu açıklaması
    public string Aciklama { get; set; } = string.Empty;

    // Sorgunun yapıldığı an (UTC)
    public DateTime SorguTarihi { get; set; }
}
