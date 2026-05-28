using System.Net;
using System.Net.Http.Json;
using BattleGrid.Contracts.ResponseDtos;

namespace BattleGrid.Tests.Integration;

public sealed class HealthEndpointTests : IClassFixture<BattleGridApiFactory>
{
    private readonly BattleGridApiFactory _factory;

    public HealthEndpointTests(BattleGridApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Root_ReturnsRunningMessage()
    {
        using var client = _factory.CreateClient();
        var body = await client.GetStringAsync("/");
        Assert.Contains("BattleGrid API is running", body);
    }

    [Fact]
    public async Task Health_ReturnsOkWithDatabaseStatus()
    {
        if (!await _factory.CanConnectToDatabaseAsync())
            return;

        using var client = _factory.CreateClient();
        var health = await client.GetFromJsonAsync<HealthResponseDto>("/health");

        Assert.NotNull(health);
        Assert.Equal("ok", health.Status);
        Assert.Equal("connected", health.Database);
    }
}