namespace BattleGrid.Domain.GameLogic;

/// <summary>
/// Columns A–J map to X = 0..9 (left to right). Rows 1–10 are numbered from the bottom: row 1 is Y = 0 (bottom edge).
/// Origin (0,0) is bottom-left; Y increases upward. Wire protocol and DB use the same convention.
/// </summary>
public static class GridCoordinates
{
    public static char ColumnIndexToLetter(int x) => (char)('A' + x);

    public static int ColumnLetterToIndex(char c)
    {
        var u = char.ToUpperInvariant(c);
        return u - 'A';
    }

    /// <summary>Row label on the board (1 = bottom row).</summary>
    public static int DisplayRowNumberFromY(int y) => y + 1;

    /// <summary>Y index from a bottom-numbered row label.</summary>
    public static int YFromDisplayRowNumber(int displayRowOneBased) => displayRowOneBased - 1;

    /// <summary>CSS / DOM row index counting from the top of the grid (0 = top row).</summary>
    public static int VisualRowFromTopFromY(int y) => Board.GridSize - 1 - y;

    /// <summary>Y index from a top-down visual row index.</summary>
    public static int YFromVisualRowFromTop(int visualRowFromTop) => Board.GridSize - 1 - visualRowFromTop;
}
