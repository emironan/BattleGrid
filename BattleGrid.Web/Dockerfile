FROM ://microsoft.com AS base
WORKDIR /app
EXPOSE 8080

FROM ://microsoft.com AS build
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

COPY ["BattleGrid.Web/wwwroot", "./wwwroot"]

ENTRYPOINT ["dotnet", "BattleGrid.Web.dll"]

