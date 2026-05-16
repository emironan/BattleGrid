using BattleGrid.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace BattleGrid.Domain.Entities;

public sealed class PlayerStat
{
    public int StatID { get; set; }
    public int UserID { get; set; }
    public int SeasonNo { get; set; } = 1;
    public int MatchesPlayed { get; set; } = 0;
    public int MatchesWon { get; set; } = 0;
    public decimal WinRate { get; set; }

    [Range(PlayerRatingBounds.Minimum, PlayerRatingBounds.Maximum)]
    public int Rating { get; set; } = PlayerRatingBounds.DefaultStartingRating;

    // Peak rating achieved during a season
    [Range(PlayerRatingBounds.Minimum, PlayerRatingBounds.Maximum)]
    public int HighestRating { get; set; } = PlayerRatingBounds.DefaultStartingRating;
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow.ToUniversalTime();
    public DateTimeOffset? LastUpdatedAt { get; set; }
}