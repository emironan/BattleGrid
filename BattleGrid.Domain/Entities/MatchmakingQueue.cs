namespace BattleGrid.Domain.Entities;

public sealed class MatchmakingQueue
{
    public int QueueID { get; set; }
    public int PlayerID { get; set; }
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

}