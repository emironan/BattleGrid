namespace BattleGrid.Contracts.ResponseDtos;

public sealed class AdvanceSeasonResponseDto
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int PreviousSeasonNo { get; init; }
    public int NewSeasonNo { get; init; }
    public int AdminStartingRating { get; init; }
}