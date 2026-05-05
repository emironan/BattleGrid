namespace BattleGrid.Contracts.ResponseDtos
{
    public class LoginResponseDto
    {
        public bool Success { get; set; } = false;
        
        public string Message { get; set; } = string.Empty;

        public string AccessToken { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;

        public DateTimeOffset ATExpiresAt { get; set; }

        public DateTimeOffset RTExpiresAt { get; set; }
    }

    /*
    public class TokenResponseDto
    {
        public string? AccessToken { get; set; }

        public string? RefreshToken { get; set; }

        public DateTimeOffset? ATExpiresAt { get; set; }

        public DateTimeOffset? RTExpiresAt { get; set; }
    }
    */
}