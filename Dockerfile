# ---- Derleme (build) aşaması ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Önce sadece proje dosyaları kopyalanır; kaynak kod değişmediği sürece
# "dotnet restore" katmanı Docker önbelleğinden gelir ve build hızlanır
COPY HavaDurumuAPI.csproj .
COPY HavaDurumuOrtak/HavaDurumuOrtak.csproj HavaDurumuOrtak/
RUN dotnet restore HavaDurumuAPI.csproj

# Kaynak kodun tamamı kopyalanır ve yayımlanır (Release modunda, restore tekrar yapılmaz)
COPY . .
RUN dotnet publish HavaDurumuAPI.csproj -c Release -o /app/publish --no-restore

# ---- Çalışma zamanı (runtime) aşaması ----
# Sadece ASP.NET Core çalışma zamanını içeren küçük imaj kullanılır (SDK'nın tamamı taşınmaz)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "HavaDurumuAPI.dll"]
