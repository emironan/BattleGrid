using BattleGrid.Domain.Enums;

namespace BattleGrid.Domain.Entities;

public sealed class User
{
    public int UserID { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsAdmin { get; set; } = false;
    public bool IsBanned { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow.ToUniversalTime();
    public DateTimeOffset? LastUpdatedAt { get; set; }
    public DateTimeOffset? LastEmailChangeAt { get; set; }
    public DateTimeOffset? LastUserNameChangeAt { get; set; }
    public string? UpdateReason { get; set; }
}