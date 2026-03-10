using BattleGrid.Domain.Enums;

namespace BattleGrid.Domain.Entities;

public sealed class User
{
    public int UserID { get; set; }
    public string? UserName { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public UserRole Role { get; set; } = UserRole.Player;
    public int Rating { get; set; } = 1000;
    public int MatchesPlayed { get; set; } = 0;
    public bool IsBanned { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastUpdatedAt { get; set; }
}