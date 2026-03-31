using System.ComponentModel.DataAnnotations;

namespace BattleGrid.Contracts.RequestDtos
{
    public class PlaceShipRequestDto
    {
        [Required]
        public int PlayerID { get; set; }

        [Required]
        public int MatchID { get; set; }
        
        [Required]
        public int ShipID { get; set; }

        [Required]
        public int StartX { get; set; }

        [Required]
        public int StartY { get; set; }

        [Required]
        public bool IsVertical { get; set; }
    }
}