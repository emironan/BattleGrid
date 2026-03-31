namespace BattleGrid.Domain.GameLogic;

public readonly struct Coordinate
{
    public int X { get; }
    public int Y { get; }

    public Coordinate(int x, int y)
    {
        if (x < 0 || x >= 10 || y < 0 || y >= 10)
            throw new ArgumentException("Coordinate out of bounds");

        X = x;
        Y = y;
    }
}