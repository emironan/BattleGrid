using BattleGrid.Domain.Enums;

namespace BattleGrid.Domain.Entities;

public sealed class PlayerStat
{
    public int StatID { get; set; }
    public int UserID { get; set; }
    public int SeasonNo { get; set; } = 1;
    public int MatchesPlayed { get; set; } = 0;
    public int MatchesWon { get; set; } = 0;
    public decimal WinRate { get; set; }
    public int Rating { get; set; } = 1000;
    public int HighestRating { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastUpdatedAt { get; set; }
}