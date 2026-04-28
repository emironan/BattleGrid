using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BattleGrid.Contracts.RequestDtos
{
    public class RegisterRequestDto
    {
        public string? UserName { get; set; }

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequestDto
    {

        // Users will not see and use UserID. We may use it with inner services
        // So, it is nullable and hidden from users while they are filling forms
        [JsonIgnore]
        public int? UserID { get; set; }

        // It can be email or username. So, named it as LoginInfo
        [Required]
        public string LoginInfo { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class PasswordUpdateRequestDto
    {
        [Required]
        [JsonIgnore]
        public int UserID { get; set; }

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string OldPassword { get; set; } = string.Empty;

        [Required]
        public string NewPassword { get; set; } = string.Empty;
    }
}