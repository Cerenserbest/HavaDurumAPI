using HavaDurumuAPI.Services;
using HavaDurumuOrtak.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Servisleri konteynere ekle

builder.Services.AddControllers();
// Swagger/OpenAPI yapılandırması hakkında daha fazla bilgi için: https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Hava durumu servisi ve bu servisin kullandığı HttpClient DI konteynerine kaydedilir
builder.Services.AddHttpClient<IHavaDurumuServisi, HavaDurumuServisi>();

// Sorgu kayıtlarını RabbitMQ kuyruğuna gönderen servis
builder.Services.AddSingleton<IKuyrukYayinciServisi, KuyrukYayinciServisi>();

// Veritabanı bağlamı, appsettings.Development.json'daki connection string ile kaydedilir.
// DbContext HavaDurumuOrtak kütüphanesinde tanımlı olsa da migration dosyaları
// bu projede (HavaDurumuAPI) tutulduğu için migrations assembly açıkça belirtiliyor.
builder.Services.AddDbContext<UygulamaDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("VarsayilanBaglanti"),
        npgsqlOptions => npgsqlOptions.MigrationsAssembly("HavaDurumuAPI")));

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
