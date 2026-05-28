using System.Net;
using System.Net.Http.Json;
using BattleGrid.Contracts.RequestDtos;
using BattleGrid.Contracts.ResponseDtos;
using BattleGrid.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BattleGrid.Tests.Integration;

public sealed class AdminEndpointTests : IClassFixture<BattleGridApiFactory>, IAsyncLifetime
{
    private readonly BattleGridApiFactory _factory;
    private HttpClient _client = null!;
    private bool _databaseAvailable;

    public AdminEndpointTests(BattleGridApiFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        _databaseAvailable = await _factory.CanConnectToDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task BanPlayer_AsNonAdmin_ReturnsForbidden()
    {
        if (!_databaseAvailable)
            return;

        TestUserSession? actor = null;
        TestUserSession? target = null;
        try
        {
            actor = await ApiIntegrationTestHelper.CreateLoggedInUserAsync(_client, _factory);
            target = await ApiIntegrationTestHelper.CreateRegisteredUserAsync(_client);

            using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(
                _factory, actor.Tokens!.AccessToken);

            using var response = await authClient.PostAsJsonAsync("/api/Admin/players/ban",
                new BanRequestDto
                {
                    PlayerInfo = target.Email,
                    BanReason = "integration test",
                    IsTemporary = true,
                    Duration = TimeSpan.FromHours(1)
                });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        finally
        {
            if (actor is not null)
                await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, actor.Email);
            if (target is not null)
                await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, target.Email);
        }
    }

    [Fact]
    public async Task GetPlayerProfile_AsAdmin_ReturnsOk()
    {
        if (!_databaseAvailable)
            return;

        TestUserSession? admin = null;
        TestUserSession? target = null;
        try
        {
            admin = await ApiIntegrationTestHelper.CreateLoggedInUserAsync(_client, _factory, asAdmin: true);
            target = await ApiIntegrationTestHelper.CreateRegisteredUserAsync(_client);
            Assert.NotNull(target.Profile);

            using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(
                _factory, admin.Tokens!.AccessToken);

            var profile = await authClient.GetFromJsonAsync<AdminUserProfileResponseDto>(
                $"/api/Admin/players/{target.Profile.UserID}/profile");

            Assert.NotNull(profile);
            Assert.Equal(target.Profile.UserID, profile.User.UserID);
            Assert.Equal(target.UserName, profile.User.UserName);
        }
        finally
        {
            if (admin is not null)
                await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, admin.Email);
            if (target is not null)
                await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, target.Email);
        }
    }

    [Fact]
    public async Task BanAndUnbanPlayer_AsAdmin_Succeeds()
    {
        if (!_databaseAvailable)
            return;

        TestUserSession? admin = null;
        TestUserSession? target = null;
        try
        {
            admin = await ApiIntegrationTestHelper.CreateLoggedInUserAsync(_client, _factory, asAdmin: true);
            target = await ApiIntegrationTestHelper.CreateRegisteredUserAsync(_client);

            using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(
                _factory, admin.Tokens!.AccessToken);

            using var banResponse = await authClient.PostAsJsonAsync("/api/Admin/players/ban",
                new BanRequestDto
                {
                    PlayerInfo = target.Email,
                    BanReason = "integration test ban",
                    IsTemporary = true,
                    Duration = TimeSpan.FromHours(1)
                });

            Assert.Equal(HttpStatusCode.OK, banResponse.StatusCode);
            var banResult = await banResponse.Content.ReadFromJsonAsync<GeneralResponseDto>();
            Assert.NotNull(banResult);
            Assert.True(banResult.Success);

            using var unbanResponse = await authClient.PostAsJsonAsync("/api/Admin/players/unban",
                new UnbanPlayerRequestDto
                {
                    PlayerInfo = target.Email,
                    Reason = "integration test unban"
                });

            Assert.Equal(HttpStatusCode.OK, unbanResponse.StatusCode);
        }
        finally
        {
            if (admin is not null)
                await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, admin.Email);
            if (target is not null)
                await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, target.Email);
        }
    }

    [Fact]
    public async Task AdvanceSeason_AsAdmin_ReturnsOk_AndCreatesSeasonRow()
    {
        if (!_databaseAvailable)
            return;

        TestUserSession? admin = null;
        try
        {
            admin = await ApiIntegrationTestHelper.CreateLoggedInUserAsync(_client, _factory, asAdmin: true);
            Assert.NotNull(admin.Profile);

            using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(
                _factory, admin.Tokens!.AccessToken);

            using var response = await ApiIntegrationTestHelper.PostAdvanceSeasonAsync(authClient);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<AdvanceSeasonResponseDto>();
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(result.PreviousSeasonNo + 1, result.NewSeasonNo);
            Assert.True(result.AdminStartingRating > 0);

            await using (var scope = _factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<BattleGridDbContext>();
                var created = await db.PlayerStat.AsNoTracking()
                    .AnyAsync(p => p.UserID == admin.Profile!.UserID && p.SeasonNo == result.NewSeasonNo);
                Assert.True(created, "Advance season should insert a PlayerStat row for the administrator.");
            }

            await ApiIntegrationTestHelper.DeletePlayerStatSeasonAsync(
                _factory, admin.Profile.UserID, result.NewSeasonNo);
        }
        finally
        {
            if (admin is not null)
                await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, admin.Email);
        }
    }

    [Fact]
    public async Task AdvanceSeason_AsNonAdmin_ReturnsForbidden()
    {
        if (!_databaseAvailable)
            return;

        TestUserSession? user = null;
        try
        {
            user = await ApiIntegrationTestHelper.CreateLoggedInUserAsync(_client, _factory);
            Assert.NotNull(user.Profile);

            var statCountBefore = await CountPlayerStatRowsAsync(user.Profile.UserID);

            using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(
                _factory, user.Tokens!.AccessToken);

            using var response = await ApiIntegrationTestHelper.PostAdvanceSeasonAsync(authClient);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            var statCountAfter = await CountPlayerStatRowsAsync(user.Profile.UserID);
            Assert.Equal(statCountBefore, statCountAfter);
        }
        finally
        {
            if (user is not null)
                await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, user.Email);
        }
    }

    [Fact]
    public async Task AdvanceSeason_WithoutAuth_ReturnsUnauthorized()
    {
        if (!_databaseAvailable)
            return;

        using var response = await ApiIntegrationTestHelper.PostAdvanceSeasonAsync(_client);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPlayerMatchHistory_AsAdmin_ReturnsOk()
    {
        if (!_databaseAvailable)
            return;

        TestUserSession? admin = null;
        TestUserSession? target = null;
        try
        {
            admin = await ApiIntegrationTestHelper.CreateLoggedInUserAsync(_client, _factory, asAdmin: true);
            target = await ApiIntegrationTestHelper.CreateRegisteredUserAsync(_client);
            Assert.NotNull(target.Profile);

            using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(
                _factory, admin.Tokens!.AccessToken);

            var history = await authClient.GetFromJsonAsync<MatchHistoryResponseDto>(
                $"/api/Admin/players/{target.Profile.UserID}/match-history?limit=5");

            Assert.NotNull(history);
            Assert.NotNull(history.Matches);
        }
        finally
        {
            if (admin is not null)
                await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, admin.Email);
            if (target is not null)
                await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, target.Email);
        }
    }

    private async Task<int> CountPlayerStatRowsAsync(int userId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BattleGridDbContext>();
        return await db.PlayerStat.CountAsync(p => p.UserID == userId);
    }
}