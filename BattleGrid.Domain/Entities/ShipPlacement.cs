namespace BattleGrid.Domain.Entities;

public sealed class ShipPlacement
{
    public int PlacementID { get; set; }
    public int PlayerID { get; set; }
    public int MatchID { get; set; }
    public int ShipID { get; set; }
    public int StartX { get; set; }
    public int StartY { get; set; }
    public bool IsVertical { get; set; }
    public DateTimeOffset PlacedAt { get; set; } = DateTimeOffset.UtcNow;
}