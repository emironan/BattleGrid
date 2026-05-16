namespace BattleGrid.Contracts.ResponseDtos;

/// <summary>How the match ended from the requesting player's perspective.</summary>
public enum MatchHistoryEndKind
{
    /// <summary>Decisive win/loss from normal gameplay (all ships destroyed).</summary>
    NormalBattle = 0,

    /// <summary>Opponent left, forfeited, disconnected, went AFK, or abandoned ship.</summary>
    OpponentLeft = 1,

    /// <summary>The requesting player left, forfeited, disconnected, went AFK, or abandoned ship.</summary>
    SelfLeft = 2,

    /// <summary>Match status is <c>Abandoned</c> (no recorded winner).</summary>
    Abandoned = 3
}

public sealed class MatchHistoryEntryResponseDto
{
    public int MatchId { get; init; }
    public string OpponentUserName { get; init; } = string.Empty;

    /// <summary><c>true</c> = win, <c>false</c> = loss, <c>null</c> = no winner (abandoned).</summary>
    public bool? PlayerWon { get; init; }

    public int RatingChange { get; init; }

    /// <summary>Domain <see cref="BattleGrid.Domain.Enums.MatchStatus"/> value.</summary>
    public int Status { get; init; }

    public MatchHistoryEndKind EndKind { get; init; }

    public DateTimeOffset? FinishedAt { get; init; }
}

public sealed class MatchHistoryResponseDto
{
    public IReadOnlyList<MatchHistoryEntryResponseDto> Matches { get; init; } = Array.Empty<MatchHistoryEntryResponseDto>();
}
