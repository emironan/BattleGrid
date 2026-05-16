using System.Diagnostics.CodeAnalysis;

namespace BattleGrid.Domain.GameLogic;

public static class ShipPlacementHelper
{
    /// <summary>
    /// Axis-aligned footprint: horizontal spans <paramref name="length"/> on X and <paramref name="width"/> on Y;
    /// vertical swaps spans (90° rotation).
    /// </summary>
    public static bool TryGenerateRectangle(
        Coordinate start,
        int length,
        int width,
        bool isVertical,
        [NotNullWhen(true)] out List<Coordinate>? coordinates)
    {
        coordinates = null;
        if (length < 1 || width < 1)
            return false;

        int spanX = isVertical ? width : length;
        int spanY = isVertical ? length : width;

        if (start.X < 0 || start.Y < 0 || start.X + spanX > Board.GridSize || start.Y + spanY > Board.GridSize)
            return false;

        coordinates = new List<Coordinate>(spanX * spanY);
        for (int dy = 0; dy < spanY; dy++)
        {
            for (int dx = 0; dx < spanX; dx++)
            {
                coordinates.Add(new Coordinate(start.X + dx, start.Y + dy));
            }
        }

        return true;
    }

    public static IEnumerable<Coordinate> Generate(
        Coordinate start,
        int length,
        bool vertical)
    {
        if (!TryGenerateRectangle(start, length, width: 1, vertical, out var list))
            throw new InvalidOperationException("Ship placement outside board.");

        return list;
    }
}
