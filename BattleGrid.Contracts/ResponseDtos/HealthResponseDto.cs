namespace BattleGrid.Contracts.ResponseDtos;

/// <summary>Public health probe for the landing page status cards.</summary>
public sealed class HealthResponseDto
{
    public string Status { get; init; } = string.Empty;

    /// <summary><c>connected</c> when the database accepts a connection; otherwise <c>unreachable</c>.</summary>
    public string Database { get; init; } = string.Empty;
}
