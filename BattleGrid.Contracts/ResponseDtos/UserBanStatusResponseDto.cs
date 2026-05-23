namespace BattleGrid.Contracts.ResponseDtos;

/// <summary>Active ban details for display under operator status.</summary>
public sealed class UserBanStatusResponseDto
{
    public bool IsBanned { get; init; }

    public bool IsPermanent { get; init; }

    public DateTimeOffset? EndsAtUtc { get; init; }

    /// <summary>Human-readable duration, e.g. <c>Permanent</c> or <c>Until 2026-05-23 14:30 UTC</c>.</summary>
    public string DisplayText { get; init; } = string.Empty;
}
