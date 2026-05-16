namespace BattleGrid.Domain.Entities;

public sealed class BanList
{
    public int BanID { get; set; }
    public int AdminID { get; set; }
    public int PlayerID { get; set; }
    public string BanReason { get; set; } = "";
    public bool IsReverted { get; set; } = false;

    // Administrator who reverted the ban; null when reverted automatically by the system.
    public int? RevertingAdminID { get; set; }
    public string? RevertingReason { get; set; }
    public bool IsTemporary { get; set; } = false;  // Permanent by default
    public DateTimeOffset BannedAt { get; set; } = DateTimeOffset.UtcNow.ToUniversalTime();

    // For temporary bans, this indicates the duration of the ban (e.g., 1 day, 1 week, etc.)
    public TimeSpan? Duration { get; set; } = TimeSpan.FromDays(36500); // Permanent by default. 100 years
    public DateTimeOffset? BannedUntil { get; set; }
}