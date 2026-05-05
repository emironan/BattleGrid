using BattleGrid.Contracts.ResponseDtos;

namespace BattleGrid.Web.Models;

public class AuthCookieSession
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTimeOffset? AccessTokenExpiry { get; set; }
    public DateTimeOffset? RefreshTokenExpiry { get; set; }
    public UserResponseDto? CurrentUser { get; set; }
}
