namespace BattleGrid.Contracts.ResponseDtos
{
    public class TokenResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTimeOffset ATExpiresAt { get; set; }
        public DateTimeOffset RTExpiresAt { get; set; }
    }
}