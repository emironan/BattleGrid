namespace BattleGrid.Contracts.ResponseDtos;

/// <summary>Season-bound competitive stats for a player (current global season row).</summary>
public sealed class PlayerSeasonStatsResponseDto
{
    public int SeasonNo { get; init; }
    public int MatchesPlayed { get; init; }
    public int MatchesWon { get; init; }
    /// <summary>Percent 0–100 (generated in database from matches).</summary>
    public decimal WinRate { get; init; }
    public int Rating { get; init; }
    /// <summary>Peak rating during the current competitive season.</summary>
    public int HighestRating { get; init; }
    public DateTimeOffset? LastUpdatedAt { get; init; }
}