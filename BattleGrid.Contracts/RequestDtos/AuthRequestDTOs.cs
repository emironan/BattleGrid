using System.ComponentModel.DataAnnotations;

namespace BattleGrid.Contracts.RequestDtos
{
    public class RegisterRequestDto
    {
        public string? UserName { get; set; } = null!;
        [Required]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequestDto
    {
        // It can be email or username. So, named it as LoginInfo
        [Required]
        public string LoginInfo { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}