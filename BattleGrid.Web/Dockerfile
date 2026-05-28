FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

# RENDER'IN PORT EZME UYARISINI BİTİREN KESİN DÜZELTME:
# Render ücretsiz planlarında portu "PORT" değişkeni üzerinden dikte eder.
# Hem HTTP_PORTS'u hem de varsayılan portları tamamen 8080'e kilitliyoruz.
ENV ASPNETCORE_HTTP_PORTS=8080
ENV PORT=8080
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
