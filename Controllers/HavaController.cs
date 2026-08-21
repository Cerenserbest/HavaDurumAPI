using HavaDurumuAPI.Services;
using HavaDurumuOrtak.Data;
using HavaDurumuOrtak.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HavaDurumuAPI.Controllers;

// Hava durumu ile ilgili istekleri karşılayan denetleyici
[ApiController]
[Route("api/hava")]
public class HavaController : ControllerBase
{
    private readonly IHavaDurumuServisi _havaDurumuServisi;
    private readonly IKuyrukYayinciServisi _kuyrukYayinciServisi;
    private readonly UygulamaDbContext _dbContext;
    private readonly ILogger<HavaController> _logger;

    public HavaController(
        IHavaDurumuServisi havaDurumuServisi,
        IKuyrukYayinciServisi kuyrukYayinciServisi,
        UygulamaDbContext dbContext,
        ILogger<HavaController> logger)
    {
        _havaDurumuServisi = havaDurumuServisi;
        _kuyrukYayinciServisi = kuyrukYayinciServisi;
        _dbContext = dbContext;
        _logger = logger;
    }

    // GET /api/hava/gecmis
    // Son 20 sorguyu tarihe göre azalan sırada döner
    [HttpGet("gecmis")]
    public async Task<IActionResult> GetGecmis()
    {
        _logger.LogInformation("Sorgu geçmişi istendi");

        var gecmis = await _dbContext.SorguKayitlari
            .OrderByDescending(kayit => kayit.SorguTarihi)
            .Take(20)
            .ToListAsync();

        return Ok(gecmis);
    }

    // GET /api/hava/{sehir}
    [HttpGet("{sehir}")]
    public async Task<IActionResult> GetHavaDurumu(string sehir)
    {
        _logger.LogInformation("'{Sehir}' için hava durumu sorgusu alındı", sehir);

        HavaDurumuSorguSonucu sonuc;
        try
        {
            sonuc = await _havaDurumuServisi.GetirAsync(sehir);
        }
        catch (HavaDurumuAlinamadiException)
        {
            // OpenWeatherMap'e ulaşılamadı (ağ hatası/zaman aşımı); detay zaten servis
            // katmanında loglandı, kullanıcıya sadece anlaşılır bir mesaj dönülür
            _logger.LogWarning("'{Sehir}' sorgusu hava durumu servisine ulaşılamadığı için tamamlanamadı", sehir);
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                "Hava durumu servisine şu anda ulaşılamıyor. Lütfen daha sonra tekrar deneyin.");
        }

        var havaDurumu = sonuc.Veri;

        // Şehir bulunamazsa kullanıcıya anlaşılır bir Türkçe hata mesajı dönülür
        if (havaDurumu is null)
        {
            _logger.LogInformation("'{Sehir}' adlı şehir bulunamadı", sehir);
            return NotFound($"'{sehir}' adlı şehir bulunamadı.");
        }

        // Sonuç cache'ten geldiyse tekrar kayıt oluşturulmaz; aksi halde aynı şehir
        // 10 dakika içinde kaç kez sorgulanırsa geçmiş o kadar şişer ve gerçek bir
        // API çağrısını temsil etmeyen tekrar kayıtlarla dolar.
        if (!sonuc.CacheTenGeldi)
        {
            // Veritabanına doğrudan yazmak yerine sorgu kaydı RabbitMQ kuyruğuna gönderilir;
            // kaydı gerçekten veritabanına yazmak HavaDurumuConsumer'ın işidir
            await _kuyrukYayinciServisi.YayinlaAsync(new SorguKaydi
            {
                Sehir = havaDurumu.Sehir,
                Sicaklik = havaDurumu.Sicaklik,
                HissedilenSicaklik = havaDurumu.HissedilenSicaklik,
                Nem = havaDurumu.Nem,
                Aciklama = havaDurumu.Aciklama,
                SorguTarihi = DateTime.UtcNow
            });
        }

        return Ok(havaDurumu);
    }
}
