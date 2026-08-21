# ---- Derleme (build) aşaması ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Önce sadece proje dosyaları kopyalanır; kaynak kod değişmediği sürece
# "dotnet restore" katmanı Docker önbelleğinden gelir ve build hızlanır
COPY HavaDurumuConsumer/HavaDurumuConsumer.csproj HavaDurumuConsumer/
COPY HavaDurumuOrtak/HavaDurumuOrtak.csproj HavaDurumuOrtak/
RUN dotnet restore HavaDurumuConsumer/HavaDurumuConsumer.csproj

# Kaynak kodun tamamı kopyalanır ve yayımlanır (Release modunda, restore tekrar yapılmaz)
COPY . .
RUN dotnet publish HavaDurumuConsumer/HavaDurumuConsumer.csproj -c Release -o /app/publish --no-restore

# ---- Çalışma zamanı (runtime) aşaması ----
# Worker Service bir web sunucusu olmadığından ASP.NET Core değil, sade .NET çalışma zamanı imajı yeterli
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "HavaDurumuConsumer.dll"]
