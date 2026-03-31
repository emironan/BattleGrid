using Microsoft.VisualBasic;

namespace BattleGrid.Domain.Entities;

public sealed class BanList
{
    public int BanID { get; set; }
    public int AdminID { get; set; }
    public int PlayerID { get; set; }
    public bool IsReverted { get; set; } = false;
    public string Reason { get; set; } = "";
    public bool IsTemporary { get; set; } = false;
    // For temporary bans, this indicates the duration of the ban (e.g., 1 day, 1 week, etc.)
    public DateTimeOffset BannedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateInterval Duration { get; set; } = DateInterval.Day;
    public DateTimeOffset? BannedUntil { get; set; }
}