using BattleGrid.Domain.Enums;

namespace BattleGrid.Contracts.ResponseDtos
{
    public class UserResponseDto
    {
        public int UserID { get; set; }

        public string? UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public bool IsAdmin { get; set; }

        public bool IsBanned { get; set; }

        public bool IsActive { get; set; }
    }
}