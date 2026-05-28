FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

# LINUX VE RENDER İÇİN KİLİT SATIR:
# .NET 10 motoruna kesin olarak 8080 portunu dinlemesini emrediyoruz.
# Bu satır olmadan Render ile Blazor Server arasındaki SignalR (Hub) bağlantısı kurulamaz.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["BattleGrid.Web/BattleGrid.Web.csproj", "BattleGrid.Web/"]
COPY ["BattleGrid.Contracts/BattleGrid.Contracts.csproj", "BattleGrid.Contracts/"]
COPY ["BattleGrid.Domain/BattleGrid.Domain.csproj", "BattleGrid.Domain/"]

RUN dotnet restore "BattleGrid.Web/BattleGrid.Web.csproj"

COPY . .
WORKDIR "/src/BattleGrid.Web"
RUN dotnet publish "BattleGrid.Web.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BattleGrid.Web.dll"]
