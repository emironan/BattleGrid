namespace BattleGrid.Contracts.RequestDtos;

/// <summary>One ship instance for hub/API. StartX/StartY are 0-based (column A = 0, display row 1 = 0).</summary>
public sealed class PlacedShipDto
{
    public int ShipID { get; set; }

    public int StartX { get; set; }

    public int StartY { get; set; }

    public bool IsVertical { get; set; }
}