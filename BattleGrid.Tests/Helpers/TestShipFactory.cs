using BattleGrid.Domain.GameLogic;

namespace BattleGrid.Tests.Helpers;

public static class TestShipFactory
{
    public static IEnumerable<Coordinate> Horizontal(int x, int y, int length)
    {
        return ShipPlacementHelper.Generate(
            new Coordinate(x, y),
            length,
            false
        );
    }

    public static IEnumerable<Coordinate> Vertical(int x, int y, int length)
    {
        return ShipPlacementHelper.Generate(
            new Coordinate(x, y),
            length,
            true
        );
    }
}