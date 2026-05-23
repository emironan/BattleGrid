using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BattleGrid.Contracts.RequestDtos
{
    // Used by users to update their UserName, Email
    public class UserUpdateUserRequestDto
    {
        [Required]
        [JsonIgnore] // We will get it from browser (AccessToken via cookie)
        public int UserID { get; set; }

        public string? UserName { get; set; }

        public string? Email { get; set; }

        public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow.ToUniversalTime();
    }

    public class ChangeEmailRequestDto
    {
        [Required]
        public string NewEmail { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class ChangeUsernameRequestDto
    {
        [Required]
        public string NewUserName { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class DeactivateAccountRequestDto
    {
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class UserUpdateAdminRequestDto
    {
        [Required]
        [JsonIgnore]
        public int AdminID { get; set; }

        [Required]
        public int UserID { get; set; }

        public string? UserName { get; set; }

        public string? BanStatus { get; set; }

        public string? Reason { get; set; }

        public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow.ToUniversalTime();
    }
}