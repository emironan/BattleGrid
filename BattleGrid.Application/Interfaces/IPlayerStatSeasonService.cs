using BattleGrid.Contracts.ResponseDtos;

namespace BattleGrid.Application.Interfaces;

/// <param name="RatingForQueue"> Rating used for matchmaking (always between 400 and 3000 inclusive). </param>
/// <param name="GlobalCurrentSeason"> Active season number (max season across all stored player stats, or 1 if none). </param>
public sealed record PlayerStatSeasonState(int RatingForQueue, int GlobalCurrentSeason);

public interface IPlayerStatSeasonService
{
    /// <summary>
    /// Loads the user's latest player stat row (by StatID). If that row is for an older season than
    /// the global current season, applies cross-season rating decay, inserts a row for the current season,
    /// and returns the new rating. If the user has no stats, creates their first row for the current season.
    /// </summary>
    Task<PlayerStatSeasonState> EnsureMatchmakingRatingAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures the user has a row for the current global season, then returns stats for that row.
    /// </summary>
    Task<PlayerSeasonStatsResponseDto> GetCurrentSeasonStatsAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Active global season number (max <c>SeasonNo</c> across all player stats, or 1 if none).</summary>
    Task<int> GetGlobalCurrentSeasonNoAsync(CancellationToken cancellationToken = default);

    /// <summary>All season stat rows for a user, newest season first.</summary>
    Task<IReadOnlyList<PlayerSeasonStatsResponseDto>> GetAllSeasonStatsForUserAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ends the current global season and starts the next by creating a new <see cref="Domain.Entities.PlayerStat"/>
    /// row for the administrator (bumps global season via max <c>SeasonNo</c>). Other players receive a row when they queue.
    /// </summary>
    Task<AdvanceSeasonResponseDto> AdvanceSeasonAsync(int adminUserId, CancellationToken cancellationToken = default);
}