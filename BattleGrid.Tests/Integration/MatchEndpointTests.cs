using System.Net;
using System.Net.Http.Json;
using BattleGrid.Contracts.ResponseDtos;
using BattleGrid.Domain.Enums;

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
    public async Task GetResumable_WithAuth_ReturnsOk_AndRelatedData()
    {
        if (!_databaseAvailable)
            return;

        TestUserSession? user = null;
        TestUserSession? opponent = null;
        int? matchId = null;
        try
        {
            user = await ApiIntegrationTestHelper.CreateLoggedInUserAsync(_client, _factory);
            opponent = await ApiIntegrationTestHelper.CreateRegisteredUserAsync(_client);
            Assert.NotNull(user.Profile);
            Assert.NotNull(opponent.Profile);

            matchId = await ApiIntegrationTestHelper.CreateResumableMatchFixtureAsync(
                _factory, user.Profile!.UserID, opponent.Profile!.UserID, MatchStatus.InProgress);

            using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(
                _factory, user.Tokens!.AccessToken);

            var dto = await authClient.GetFromJsonAsync<ResumableMatchResponseDto>("/api/Match/resumable");
            Assert.NotNull(dto);
            Assert.Equal(matchId.Value, dto.MatchId);
        }
        finally
        {
            if (matchId is int createdMatchId)
                await ApiIntegrationTestHelper.DeleteMatchFixtureAsync(_factory, createdMatchId);
            if (user is not null)
                await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, user.Email);
            if (opponent is not null)
                await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, opponent.Email);
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
    public async Task GetHistory_WithoutAuth_ReturnsUnauthorized()
    {
        if (!_databaseAvailable)
            return;

        using var response = await _client.GetAsync("/api/Match/history?limit=10");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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
    public async Task GetReplay_WithAuth_ReturnsOk()
    {
        if (!_databaseAvailable)
            return;

        TestUserSession? player1 = null;
        TestUserSession? player2 = null;
        int? matchId = null;
        try
        {
            player1 = await ApiIntegrationTestHelper.CreateLoggedInUserAsync(_client, _factory);
            player2 = await ApiIntegrationTestHelper.CreateRegisteredUserAsync(_client);
            Assert.NotNull(player1.Profile);
            Assert.NotNull(player2.Profile);

            matchId = await ApiIntegrationTestHelper.CreateReplayableMatchFixtureAsync(
                _factory, player1.Profile!.UserID, player2.Profile!.UserID);

            using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(
                _factory, player1.Tokens!.AccessToken);

            var replay = await authClient.GetFromJsonAsync<MatchReplayResponseDto>(
                $"/api/Match/{matchId.Value}/replay");

            Assert.NotNull(replay);
            Assert.Equal(matchId.Value, replay.MatchId);
            Assert.Equal(player1.Profile.UserID, replay.Player1Id);
            Assert.Equal(player2.Profile.UserID, replay.Player2Id);
            Assert.NotEmpty(replay.Moves);
            Assert.NotEmpty(replay.Placements);
        }
        finally
        {
            if (matchId is int createdMatchId)
                await ApiIntegrationTestHelper.DeleteMatchFixtureAsync(_factory, createdMatchId);
            if (player1 is not null)
                await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, player1.Email);
            if (player2 is not null)
                await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, player2.Email);
        }
    }

    [Fact]
    public async Task GetReplay_WithoutAuth_ReturnsUnauthorized()
    {
        if (!_databaseAvailable)
            return;

        using var response = await _client.GetAsync("/api/Match/999999/replay");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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

    [Fact]
    public async Task GetRecovery_WithAuth_ReturnsOk_AndMatchData()
    {
        if (!_databaseAvailable)
            return;

        TestUserSession? user = null;
        TestUserSession? opponent = null;
        int? matchId = null;
        try
        {
            user = await ApiIntegrationTestHelper.CreateLoggedInUserAsync(_client, _factory);
            opponent = await ApiIntegrationTestHelper.CreateRegisteredUserAsync(_client);
            Assert.NotNull(user.Profile);
            Assert.NotNull(opponent.Profile);

            matchId = await ApiIntegrationTestHelper.CreateRecoveryMatchFixtureAsync(
                _factory, user.Profile!.UserID, opponent.Profile!.UserID, MatchStatus.P1Won);

            using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(
                _factory, user.Tokens!.AccessToken);

            var recovery = await authClient.GetFromJsonAsync<MatchRecoveryStateDto>(
                $"/api/Match/{matchId.Value}/recovery");

            Assert.NotNull(recovery);
            Assert.Equal(matchId.Value, recovery.MatchId);
            Assert.Equal((int)MatchStatus.P1Won, recovery.Status);
            Assert.Equal(user.Profile.UserID, recovery.Player1Id);
            Assert.Equal(opponent.Profile.UserID, recovery.Player2Id);
            Assert.Equal(user.Profile.UserID, recovery.WinnerUserId);
        }
        finally
        {
            if (matchId is int createdMatchId)
                await ApiIntegrationTestHelper.DeleteMatchFixtureAsync(_factory, createdMatchId);
            if (user is not null)
                await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, user.Email);
            if (opponent is not null)
                await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, opponent.Email);
        }
    }

    [Fact]
    public async Task GetRecovery_WithoutAuth_ReturnsUnauthorized()
    {
        if (!_databaseAvailable)
            return;

        using var response = await _client.GetAsync("/api/Match/999999/recovery");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}