using HavaDurumuAPI.Data;
using HavaDurumuAPI.Models;
using HavaDurumuAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HavaDurumuAPI.Controllers;

// Hava durumu ile ilgili istekleri karşılayan denetleyici
[ApiController]
[Route("api/hava")]
public class HavaController : ControllerBase
{
    private readonly IHavaDurumuServisi _havaDurumuServisi;
    private readonly UygulamaDbContext _dbContext;

    public HavaController(IHavaDurumuServisi havaDurumuServisi, UygulamaDbContext dbContext)
    {
        _havaDurumuServisi = havaDurumuServisi;
        _dbContext = dbContext;
    }

    // GET /api/hava/gecmis
    // Son 20 sorguyu tarihe göre azalan sırada döner
    [HttpGet("gecmis")]
    public async Task<IActionResult> GetGecmis()
    {
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
        var havaDurumu = await _havaDurumuServisi.GetirAsync(sehir);

        // Şehir bulunamazsa kullanıcıya anlaşılır bir Türkçe hata mesajı dönülür
        if (havaDurumu is null)
        {
            return NotFound($"'{sehir}' adlı şehir bulunamadı.");
        }

        // Başarılı sorgu veritabanına kaydedilir
        _dbContext.SorguKayitlari.Add(new SorguKaydi
        {
            Sehir = havaDurumu.Sehir,
            Sicaklik = havaDurumu.Sicaklik,
            HissedilenSicaklik = havaDurumu.HissedilenSicaklik,
            Nem = havaDurumu.Nem,
            Aciklama = havaDurumu.Aciklama,
            SorguTarihi = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        return Ok(havaDurumu);
    }
}
