using System.Net;
using System.Net.Http.Json;
using BattleGrid.Contracts.RequestDtos;

namespace BattleGrid.Tests.Integration;

public sealed class ShipPlacementEndpointTests : IClassFixture<BattleGridApiFactory>, IAsyncLifetime
{
    private readonly BattleGridApiFactory _factory;
    private HttpClient _client = null!;
    private bool _databaseAvailable;

    public ShipPlacementEndpointTests(BattleGridApiFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        _databaseAvailable = await _factory.CanConnectToDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task PlaceShip_WithoutAuth_ReturnsUnauthorized()
    {
        if (!_databaseAvailable)
            return;

        using var response = await _client.PostAsJsonAsync("/api/ShipPlacement/placeShip",
            new PlaceShipRequestDto
            {
                PlayerID = 1,
                MatchID = 1,
                ShipID = 1,
                StartX = 0,
                StartY = 0,
                IsVertical = false
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PlaceShip_WithAuth_AndInvalidMatch_ReturnsUnauthorized()
    {
        if (!_databaseAvailable)
            return;

        TestUserSession? session = null;
        try
        {
            session = await ApiIntegrationTestHelper.CreateLoggedInUserAsync(_client, _factory);
            Assert.NotNull(session.Profile);

            using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(
                _factory, session.Tokens!.AccessToken);

            using var response = await authClient.PostAsJsonAsync("/api/ShipPlacement/placeShip",
                new PlaceShipRequestDto
                {
                    PlayerID = session.Profile!.UserID,
                    MatchID = 999999,
                    ShipID = 1,
                    StartX = 0,
                    StartY = 0,
                    IsVertical = false
                });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            if (session is not null)
                await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, session.Email);
        }
    }
}