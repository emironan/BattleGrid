using Microsoft.VisualBasic;

namespace BattleGrid.Domain.Entities;

public sealed class BanList
{
    public int BanID { get; set; }
    public int AdminID { get; set; }
    public int PlayerID { get; set; }
    public bool IsReverted { get; set; } = false;

    // Default 0 for service operations they revert a temporary ban after its duration has ended.
    // If we see 0 as RevertingAdminID for a permanent ban, we have a problem!
    public int? RevertingAdminID { get; set; }
    public string Reason { get; set; } = "";
    public bool IsTemporary { get; set; } = false;
    // For temporary bans, this indicates the duration of the ban (e.g., 1 day, 1 week, etc.)
    public DateTimeOffset BannedAt { get; set; } = DateTimeOffset.UtcNow;
    public TimeSpan? Duration { get; set; } = TimeSpan.FromDays(1);
    public DateTimeOffset? BannedUntil { get; set; }
}