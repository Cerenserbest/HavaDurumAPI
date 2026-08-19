using HavaDurumuAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HavaDurumuAPI.Controllers;

// Hava durumu ile ilgili istekleri karşılayan denetleyici
[ApiController]
[Route("api/hava")]
public class HavaController : ControllerBase
{
    private readonly IHavaDurumuServisi _havaDurumuServisi;

    public HavaController(IHavaDurumuServisi havaDurumuServisi)
    {
        _havaDurumuServisi = havaDurumuServisi;
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

        return Ok(havaDurumu);
    }
}
