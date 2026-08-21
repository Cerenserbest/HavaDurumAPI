using Microsoft.AspNetCore.Diagnostics;

namespace HavaDurumuAPI.Middleware;

// Controller içinde ele alınmayan tüm beklenmeyen hataları yakalayan global handler.
// Kullanıcıya iç detaylar (stack trace, mesaj vb.) sızdırılmaz; tam detay sadece loga yazılır.
public class GenelHataYakalayici : IExceptionHandler
{
    private readonly ILogger<GenelHataYakalayici> _logger;

    public GenelHataYakalayici(ILogger<GenelHataYakalayici> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "Beklenmeyen bir hata oluştu. Yol: {Yol}",
            httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/json; charset=utf-8";

        await httpContext.Response.WriteAsJsonAsync(
            new { hata = "Sunucuda beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyin." },
            cancellationToken);

        return true;
    }
}
