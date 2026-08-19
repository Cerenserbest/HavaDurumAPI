using Microsoft.AspNetCore.Mvc;

namespace HavaDurumuAPI.Controllers;

// Hava durumu ile ilgili istekleri karşılayan denetleyici (şimdilik iskelet)
[ApiController]
[Route("api/hava")]
public class HavaController : ControllerBase
{
    // GET /api/hava/{sehir}
    // Şimdilik gerçek hava durumu verisi yok, sadece sabit bir metin dönüyor
    [HttpGet("{sehir}")]
    public IActionResult GetHavaDurumu(string sehir)
    {
        return Ok($"Merhaba {sehir}");
    }
}
