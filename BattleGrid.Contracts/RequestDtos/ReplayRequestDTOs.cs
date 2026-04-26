using System.ComponentModel.DataAnnotations;

namespace BattleGrid.Contracts.RequestDtos
{
    public class StartReplayRequestDto
    {
        // PlayerID will be used to determine if the player played the match with the MatchID below OR if the player is an admin
        [Required]
        public int PlayerID { get; set; } 
        [Required]
        public int MatchID { get; set; }
    }
}