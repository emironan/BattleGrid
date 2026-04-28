using System.ComponentModel.DataAnnotations;

namespace BattleGrid.Contracts.ResponseDtos
{
    public class GeneralResponseDto
    {
        [Required]
        public bool Success { get; set; }

        public string Message { get; set; } = null!;
    }
}