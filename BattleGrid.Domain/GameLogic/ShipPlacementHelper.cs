namespace BattleGrid.Domain.GameLogic;

public static class ShipPlacementHelper
{
    private const int BoardSize = 10;

    public static IEnumerable<Coordinate> Generate(
        Coordinate start,
        int length,
        bool vertical)
    {
        var coordinates = new List<Coordinate>();

        for (int i = 0; i < length; i++)
        {
            int x = vertical ? start.X : start.X + i;
            int y = vertical ? start.Y + i : start.Y;

            if (x >= BoardSize || y >= BoardSize)
                throw new InvalidOperationException("Ship placement outside board.");

            coordinates.Add(new Coordinate(x, y));
        }

        return coordinates;
    }
}