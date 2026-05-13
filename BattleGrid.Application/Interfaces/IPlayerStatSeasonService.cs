namespace BattleGrid.Application.Interfaces;

/// <param name="RatingForQueue"> Rating used for matchmaking (positive). </param>
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
}
