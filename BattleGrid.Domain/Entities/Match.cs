using BattleGrid.Domain.Enums;

namespace BattleGrid.Domain.Entities;

public sealed class Match
{
    public int MatchID { get; set; }
    public int Player1ID { get; set; }
    public int Player2ID { get; set; }
    public int? P1RatingChange { get; set; }
    public int? P2RatingChange { get; set; }
    public MatchStatus Status { get; set; } = MatchStatus.Loading;
    public int TotalNoOfTurns { get; set; } = 0;
    public string? FinishReason { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FinishedAt { get; set; }
}