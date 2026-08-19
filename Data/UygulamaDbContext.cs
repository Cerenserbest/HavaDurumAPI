using HavaDurumuAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HavaDurumuAPI.Data;

// Uygulamanın veritabanı bağlamı
public class UygulamaDbContext : DbContext
{
    public UygulamaDbContext(DbContextOptions<UygulamaDbContext> options) : base(options)
    {
    }

    // Yapılan hava durumu sorgularının kaydedildiği tablo
    public DbSet<SorguKaydi> SorguKayitlari => Set<SorguKaydi>();
}
