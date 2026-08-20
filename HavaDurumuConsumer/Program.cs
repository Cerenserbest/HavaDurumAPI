using HavaDurumuConsumer;
using HavaDurumuOrtak.Data;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

// Worker Service, ASP.NET Core gibi ortam bazlı dosya yüklemeyi otomatik yapmadığından
// geliştirmeye özel ayarlar (veritabanı bağlantı bilgisi gibi) burada elle ekleniyor.
// Bu dosya .gitignore'da olduğu için hassas bilgiler depoya gitmiyor.
builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

// Veritabanı bağlamı, API'nin kullandığı aynı veritabanına yazmak için kaydedilir
builder.Services.AddDbContext<UygulamaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("VarsayilanBaglanti")));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
