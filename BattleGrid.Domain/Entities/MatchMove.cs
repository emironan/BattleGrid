namespace BattleGrid.Domain.Entities;

    public sealed class MatchMove
    {
        public int MoveID { get; set; }
        public int PlayerID { get; set; }
        public int MatchID { get; set; }
        public int MoveNumber { get; set; }
        public int HitX { get; set; }
        public int HitY { get; set; }
        public bool Result { get; set; }
        public DateTimeOffset TimeOfMove { get; set; } = DateTimeOffset.UtcNow;
    }