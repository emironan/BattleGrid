namespace BattleGrid.Contracts.ResponseDtos;

/// <summary> Result of finalizing a battle in the database, including rating deltas shown to clients. </summary>
public sealed class PersistCompletedBattleResponseDto
{
    public bool Success { get; init; }

    public string Message { get; init; } = "";

    /// <summary> Applied change for player 1 (positive win, negative loss). Meaningful when <see cref="Success"/> is true. </summary>
    public int Player1RatingChange { get; init; }

    /// <summary> Applied change for player 2 (positive win, negative loss). Meaningful when <see cref="Success"/> is true. </summary>
    public int Player2RatingChange { get; init; }
}