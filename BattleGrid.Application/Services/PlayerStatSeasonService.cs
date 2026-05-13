using BattleGrid.Application.Interfaces;
using BattleGrid.Domain.Entities;
using BattleGrid.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BattleGrid.Application.Services;

public sealed class PlayerStatSeasonService : IPlayerStatSeasonService
{
    private readonly BattleGridDbContext _context;

    public PlayerStatSeasonService(BattleGridDbContext context)
    {
        _context = context;
    }

    public async Task<PlayerStatSeasonState> EnsureMatchmakingRatingAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var currentSeason = await GetGlobalCurrentSeasonNoAsync(cancellationToken);

        var latest = await _context.PlayerStat.AsNoTracking()
            .Where(p => p.UserID == userId)
            .OrderByDescending(p => p.StatID)
            .FirstOrDefaultAsync(cancellationToken);

        if (latest is null)
        {
            const int rating = 1000;
            _context.PlayerStat.Add(new PlayerStat
            {
                UserID = userId,
                SeasonNo = currentSeason,
                MatchesPlayed = 0,
                MatchesWon = 0,
                WinRate = 0,
                Rating = rating,
                HighestRating = rating,
                CreatedAt = DateTimeOffset.UtcNow,
                LastUpdatedAt = DateTimeOffset.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);
            return new PlayerStatSeasonState(Math.Max(1, rating), currentSeason);
        }

        if (latest.SeasonNo == currentSeason)
            return new PlayerStatSeasonState(Math.Max(1, latest.Rating), currentSeason);

        if (latest.SeasonNo > currentSeason)
        {
            // Should not occur if global season is derived from the same table; treat as authoritative.
            return new PlayerStatSeasonState(Math.Max(1, latest.Rating), latest.SeasonNo);
        }

        var seasonsBehind = currentSeason - latest.SeasonNo;
        var raw = latest.Rating - latest.Rating * 0.20m * seasonsBehind;
        var adjusted = (int)Math.Round(raw);
        adjusted = Math.Max(400, adjusted);

        _context.PlayerStat.Add(new PlayerStat
        {
            UserID = userId,
            SeasonNo = currentSeason,
            MatchesPlayed = 0,
            MatchesWon = 0,
            WinRate = 0,
            Rating = adjusted,
            HighestRating = Math.Max(adjusted, latest.HighestRating),
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);
        return new PlayerStatSeasonState(Math.Max(1, adjusted), currentSeason);
    }

    private async Task<int> GetGlobalCurrentSeasonNoAsync(CancellationToken cancellationToken)
    {
        if (!await _context.PlayerStat.AsNoTracking().AnyAsync(cancellationToken))
            return 1;

        return await _context.PlayerStat.AsNoTracking().MaxAsync(p => p.SeasonNo, cancellationToken);
    }
}
