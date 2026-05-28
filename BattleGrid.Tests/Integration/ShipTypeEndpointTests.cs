using System.Net.Http.Json;
using BattleGrid.Contracts.ResponseDtos;

namespace BattleGrid.Tests.Integration;

public sealed class ShipTypeEndpointTests : IClassFixture<BattleGridApiFactory>
{
    private readonly BattleGridApiFactory _factory;

    public ShipTypeEndpointTests(BattleGridApiFactory factory) => _factory = factory;

    [Fact]
    public async Task GetAll_ReturnsShipList()
    {
        if (!await _factory.CanConnectToDatabaseAsync())
            return;

        using var client = _factory.CreateClient();
        var ships = await client.GetFromJsonAsync<List<ShipTypeListResponseDto>>("/api/ShipType/all");

        Assert.NotNull(ships);
        Assert.NotEmpty(ships);
        Assert.All(ships, s =>
        {
            Assert.False(string.IsNullOrWhiteSpace(s.ShipName));
            Assert.True(s.Length > 0);
            Assert.True(s.MaxPerPlayer > 0);
        });
    }
}