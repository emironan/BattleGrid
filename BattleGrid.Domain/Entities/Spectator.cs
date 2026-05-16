using Microsoft.VisualBasic;

namespace BattleGrid.Domain.Entities;

public sealed class Spectator
{
    public int SpectatorID { get; set; }
    public int MatchID { get; set; }
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow.ToUniversalTime();
    public DateInterval Duration { get; set; } = DateInterval.Minute;
}