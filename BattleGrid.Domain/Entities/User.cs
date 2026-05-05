using BattleGrid.Domain.Enums;

namespace BattleGrid.Domain.Entities;

public sealed class User
{
    public int UserID { get; set; }
    public string? UserName { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool IsAdmin { get; set; } = false;
    public bool IsBanned { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastUpdatedAt { get; set; }
    public string? UpdateReason { get; set; }
}