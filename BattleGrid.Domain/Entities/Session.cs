namespace BattleGrid.Domain.Entities;

public sealed class Session
{
    public int SessionID { get; set; }
    public int UserID { get; set; }
    public string RefreshToken { get; set; } = "";
    public DateTimeOffset RT_ExpiresAt { get; set; }
    public string AccessToken { get; set; } = "";
    public DateTimeOffset AT_ExpiresAt { get; set; }
    public DateTimeOffset LastLogin { get; set; }
    public bool IsRevoked { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastUpdatedAt { get; set; }
}