# HavaDurumuAPI

Şehir adına göre anlık hava durumu bilgisi sunan, sorgu geçmişini asenkron olarak
veritabanına kaydeden bir ASP.NET Core Web API projesi. Hava durumu verisi
[OpenWeatherMap](https://openweathermap.org/) üzerinden çekilir, kısa süreli
olarak cache'lenir ve her gerçek API çağrısı RabbitMQ üzerinden ayrı bir
consumer servisine iletilerek PostgreSQL'e yazılır.

## Proje Ne Yapıyor

- `GET /api/hava/{sehir}` isteği geldiğinde:
  1. Sonuç 10 dakika içinde daha önce sorulmuşsa cache'ten döner (dış servise istek atılmaz).
  2. Cache'te yoksa OpenWeatherMap'ten gerçek zamanlı veri çekilir.
  3. Gerçek API çağrısının sonucu RabbitMQ kuyruğuna mesaj olarak gönderilir; kullanıcıya hemen yanıt dönülür.
  4. Ayrı bir arka plan servisi (consumer) bu mesajı kuyruktan alıp veritabanına kaydeder.
- `GET /api/hava/gecmis` ile veritabanına kaydedilmiş son 20 sorgu (en yeniden en eskiye) listelenebilir.
- OpenWeatherMap'e ulaşılamazsa (ağ hatası/zaman aşımı) `503`, şehir bulunamazsa `404` döner; beklenmeyen tüm hatalar global bir hata yakalayıcı tarafından yakalanıp kullanıcıya güvenli bir mesajla, detayları ise sadece loglara yazılarak iletilir.

## Kullanılan Teknolojiler

| Katman | Teknoloji |
|---|---|
| API | ASP.NET Core 8 (Controller tabanlı) |
| Hava durumu verisi | OpenWeatherMap REST API |
| Cache | `IMemoryCache` (10 dakika) |
| Mesaj kuyruğu | RabbitMQ (`RabbitMQ.Client` 7.x) |
| Veritabanı | PostgreSQL 16 |
| ORM | Entity Framework Core 8 (Npgsql sağlayıcısı) |
| Arka plan servisi | .NET Worker Service (`HavaDurumuConsumer`) |
| Konteynerleştirme | Docker, Docker Compose |
| Gizli bilgi yönetimi | `dotnet user-secrets` (yerel geliştirme), `.env` dosyası (Docker Compose) |

## Proje Yapısı

```
HavaDurumuAPI.sln
├── HavaDurumuAPI/          → Web API (kök dizinde), Controllers / Services / Models
├── HavaDurumuOrtak/        → API ve Consumer'ın paylaştığı class library (entity + DbContext)
└── HavaDurumuConsumer/     → RabbitMQ kuyruğunu dinleyip veritabanına yazan Worker Service
```

## Mimari

```
┌──────────┐      GET /api/hava/{sehir}      ┌─────────────────────┐
│ Kullanıcı │ ─────────────────────────────▶ │     HavaDurumuAPI    │
└──────────┘                                  │  (ASP.NET Core Web  │
                                               │        API)         │
                     10 dk cache ──┐           │                     │
                     (varsa dön)   └────────▶  │  IMemoryCache        │
                                               │  OpenWeatherMap'e    │
                                               │  gerçek çağrı        │
                                               └─────────┬───────────┘
                                                          │ JSON mesaj
                                                          │ (yalnızca gerçek
                                                          │  API çağrılarında)
                                                          ▼
                                               ┌─────────────────────┐
                                               │      RabbitMQ        │
                                               │ "havadurumu-kuyruk"  │
                                               └─────────┬───────────┘
                                                          │ tüketir
                                                          ▼
                                               ┌─────────────────────┐
                                               │  HavaDurumuConsumer   │
                                               │   (Worker Service)   │
                                               └─────────┬───────────┘
                                                          │ INSERT
                                                          ▼
                                               ┌─────────────────────┐
                                               │     PostgreSQL        │
                                               │   SorguKayitlari      │
                                               └─────────────────────┘
```

`GET /api/hava/gecmis` isteği ise API'den doğrudan PostgreSQL'e okuma yapar
(kuyruğa uğramaz).

## Nasıl Çalıştırılır

### Gereksinimler

- [Docker](https://www.docker.com/) ve Docker Compose
- Bir [OpenWeatherMap API anahtarı](https://openweathermap.org/api) (ücretsiz plan yeterli)

### 1. API anahtarını tanımla

Proje kökünde `.env.example` dosyasını `.env` olarak kopyala ve kendi
OpenWeatherMap API anahtarını gir:

```bash
copy .env.example .env
```

`.env` içeriği:

```
OPENWEATHER_API_KEY=<kendi_api_anahtarınız>
```

> `.env` dosyası `.gitignore`'da olduğu için depoya gönderilmez. Docker
> Compose bu dosyayı otomatik olarak okur ve `docker-compose.yml` içindeki
> `${OPENWEATHER_API_KEY}` yer tutucusunun yerine koyar.

### 2. Tüm sistemi başlat

```bash
docker compose up -d --build
```

Bu komut sırayla:
- `postgres` ve `rabbitmq` servislerini başlatır ve sağlıklı olmalarını bekler,
- ardından `api` ve `consumer` servislerini build edip başlatır,
- API açılışta bekleyen Entity Framework migration'larını **otomatik** uygular (elle bir işlem gerekmez).

### 3. Servislerin durumunu kontrol et

```bash
docker compose ps
```

| Servis | Adres |
|---|---|
| API | http://localhost:5182 |
| Swagger (Development ortamında) | http://localhost:5182/swagger |
| RabbitMQ yönetim paneli | http://localhost:15672 (guest / guest) |
| PostgreSQL | localhost:5432 (havadurumu / havadurumu123 / havadurumu_db) |

### 4. Sistemi durdur

```bash
docker compose down
```

Veritabanı verisi `postgres-data` adlı named volume'da kalıcı olarak saklanır
(`docker compose down -v` verilerle birlikte volume'u da siler).

### Docker olmadan yerel geliştirme

API ve Consumer'ı Docker kullanmadan çalıştırmak isterseniz:

1. PostgreSQL ve RabbitMQ'yu yerel olarak (veya `docker compose up postgres rabbitmq` ile) ayağa kaldırın.
2. API projesinde OpenWeatherMap anahtarını user-secrets ile tanımlayın:
   ```bash
   dotnet user-secrets set "OpenWeather:ApiKey" "<kendi_api_anahtarınız>"
   ```
3. Bağlantı bilgilerini `appsettings.Development.json` içinde tanımlayın (bu dosya `.gitignore`'dadır).
4. `dotnet run` ile önce API'yi, ardından `HavaDurumuConsumer` klasöründe Consumer'ı başlatın.

## Endpoint'ler

| Metod | Yol | Açıklama | Yanıtlar |
|---|---|---|---|
| `GET` | `/api/hava/{sehir}` | Belirtilen şehir için anlık hava durumu bilgisini döner (cache'ten veya OpenWeatherMap'ten) | `200` başarılı, `404` şehir bulunamadı, `503` hava durumu servisine ulaşılamadı |
| `GET` | `/api/hava/gecmis` | Veritabanına kaydedilmiş son 20 sorguyu tarihe göre azalan sırada döner | `200` başarılı |

### Örnek istek/yanıt

```
GET /api/hava/Istanbul

200 OK
{
  "sehir": "İstanbul",
  "sicaklik": 29.78,
  "hissedilenSicaklik": 34.97,
  "nem": 72,
  "aciklama": "parçalı bulutlu"
}
```

```
GET /api/hava/gecmis

200 OK
[
  {
    "id": 6,
    "sehir": "İstanbul",
    "sicaklik": 29.78,
    "hissedilenSicaklik": 34.97,
    "nem": 72,
    "aciklama": "parçalı bulutlu",
    "sorguTarihi": "2026-08-19T12:14:32.499853Z"
  }
]
```
