using System.Net;
using System.Net.Http.Json;
using BattleGrid.Contracts.ResponseDtos;

namespace BattleGrid.Tests.Integration;

public sealed class LeaderboardEndpointTests : IClassFixture<BattleGridApiFactory>
{
    private readonly BattleGridApiFactory _factory;

    public LeaderboardEndpointTests(BattleGridApiFactory factory) => _factory = factory;

    [Fact]
    public async Task GetCurrentSeasonLeaderboard_ReturnsOkAndValidEntries()
    {
        if (!await _factory.CanConnectToDatabaseAsync())
            return;

        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/api/Leaderboard/season");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var entries = await response.Content.ReadFromJsonAsync<List<LeaderboardEntryDto>>();
        Assert.NotNull(entries);

        Assert.All(entries, entry =>
        {
            Assert.True(entry.Rank > 0);
            Assert.False(string.IsNullOrWhiteSpace(entry.UserName));
            Assert.True(entry.Rating >= 0);
            Assert.True(entry.MatchesPlayed >= 0);
            Assert.True(entry.MatchesWon >= 0);
            Assert.True(entry.SeasonNo >= 1);
        });
    }

    [Fact]
    public async Task GetAllTimeLeaderboard_ReturnsOkAndValidEntries()
    {
        if (!await _factory.CanConnectToDatabaseAsync())
            return;

        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/api/Leaderboard/all-time");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var entries = await response.Content.ReadFromJsonAsync<List<LeaderboardEntryDto>>();
        Assert.NotNull(entries);

        Assert.All(entries, entry =>
        {
            Assert.True(entry.Rank > 0);
            Assert.False(string.IsNullOrWhiteSpace(entry.UserName));
            Assert.True(entry.Rating >= 0);
            Assert.True(entry.MatchesPlayed >= 0);
            Assert.True(entry.MatchesWon >= 0);
            Assert.True(entry.SeasonNo >= 1);
        });
    }
}