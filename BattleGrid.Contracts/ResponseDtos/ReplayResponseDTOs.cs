using System.ComponentModel.DataAnnotations;

namespace BattleGrid.Contracts.ResponseDtos
{
    public class ReplayShipPlacementResponseDto
    {
        public int PlayerID { get; set; } // Which player placed it

        public int PlacementID { get; set; } // Determine placement order

        public int ShipID { get; set; } // Which ship

        public int StartX { get; set; } // Where

        public int StartY { get; set; }

        public bool IsVertical { get; set; }
        // Did not include MatchID because replay request will be for a specific match everytime
    }

    public class ReplayMatchMoveResponseDto
    {
        public int PlayerID { get; set; } // Which player fired a shot

        public int MoveID { get; set; } // Determine move order

        public int HitX { get; set; } // Where

        public int HitY { get; set; }

        public bool Result { get; set; } // Hit/Miss
        // Did not include MatchID because replay request will be for a specific match everytime
    }
}