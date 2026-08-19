using HavaDurumuAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Servisleri konteynere ekle

builder.Services.AddControllers();
// Swagger/OpenAPI yapılandırması hakkında daha fazla bilgi için: https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Hava durumu servisi ve bu servisin kullandığı HttpClient DI konteynerine kaydedilir
builder.Services.AddHttpClient<IHavaDurumuServisi, HavaDurumuServisi>();

var app = builder.Build();

// HTTP istek pipeline'ını yapılandır
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
