namespace BattleGrid.Domain.GameLogic;

/// <summary>
/// Board coordinates: (0,0) is bottom-left, X increases to the right, Y increases upward.
/// </summary>
public readonly struct Coordinate
{
    public int X { get; }
    public int Y { get; }

    public Coordinate(int x, int y)
    {
        if (x < 0 || x >= Board.GridSize || y < 0 || y >= Board.GridSize)
            throw new ArgumentException("Coordinate out of bounds");

        X = x;
        Y = y;
    }
}