using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BattleGrid.Contracts.RequestDtos
{
    public class StartReplayRequestDto
    {
        // PlayerID will be used to determine if this player actually played in the match with the MatchID below
        //  OR if the requesting player is an admin
        // This will be taken from Route, hidden from users for security
        [JsonIgnore]
        [Required]
        public int PlayerID { get; set; }

        // The plan is to show the user a page with their match history.
        // When they choose a match, we should take its MatchID and put it here.
        // User should not be searching for MatchID
        [Required]
        public int MatchID { get; set; }
    }
}