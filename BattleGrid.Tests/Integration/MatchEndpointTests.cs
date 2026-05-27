using System.Net;
using System.Net.Http.Json;
using BattleGrid.Contracts.ResponseDtos;

namespace BattleGrid.Tests.Integration;

public sealed class MatchEndpointTests : IClassFixture<BattleGridApiFactory>, IAsyncLifetime
{
    private readonly BattleGridApiFactory _factory;
    private HttpClient _client = null!;
    private bool _databaseAvailable;

    public MatchEndpointTests(BattleGridApiFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        _databaseAvailable = await _factory.CanConnectToDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetResumable_WithAuth_AndNoActiveMatch_ReturnsNoContent()
    {
        if (!_databaseAvailable)
            return;

        TestUserSession? session = null;
        try
        {
            session = await ApiIntegrationTestHelper.CreateLoggedInUserAsync(_client, _factory);
            using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(
                _factory, session.Tokens!.AccessToken);

            using var response = await authClient.GetAsync("/api/Match/resumable");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
        finally
        {
            if (session is not null)
                await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, session.Email);
        }
    }

    [Fact]
    public async Task GetResumable_WithoutAuth_ReturnsUnauthorized()
    {
        if (!_databaseAvailable)
            return;

        using var response = await _client.GetAsync("/api/Match/resumable");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetHistory_WithAuth_ReturnsOk()
    {
        if (!_databaseAvailable)
            return;

        TestUserSession? session = null;
        try
        {
            session = await ApiIntegrationTestHelper.CreateLoggedInUserAsync(_client, _factory);
            using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(
                _factory, session.Tokens!.AccessToken);

            var history = await authClient.GetFromJsonAsync<MatchHistoryResponseDto>(
                "/api/Match/history?limit=10");

            Assert.NotNull(history);
            Assert.NotNull(history.Matches);
        }
        finally
        {
            if (session is not null)
                await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, session.Email);
        }
    }

    [Fact]
    public async Task GetReplay_ForUnknownMatch_ReturnsNotFound()
    {
        if (!_databaseAvailable)
            return;

        TestUserSession? session = null;
        try
        {
            session = await ApiIntegrationTestHelper.CreateLoggedInUserAsync(_client, _factory);
            using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(
                _factory, session.Tokens!.AccessToken);

            using var response = await authClient.GetAsync("/api/Match/999999/replay");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        finally
        {
            if (session is not null)
                await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, session.Email);
        }
    }

    [Fact]
    public async Task GetRecovery_ForUnknownMatch_ReturnsNoContent()
    {
        if (!_databaseAvailable)
            return;

        TestUserSession? session = null;
        try
        {
            session = await ApiIntegrationTestHelper.CreateLoggedInUserAsync(_client, _factory);
            using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(
                _factory, session.Tokens!.AccessToken);

            using var response = await authClient.GetAsync("/api/Match/999999/recovery");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
        finally
        {
            if (session is not null)
                await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, session.Email);
        }
    }
}