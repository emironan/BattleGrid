using BattleGrid.Contracts.RequestDtos;

namespace BattleGrid.Contracts.ResponseDtos;

public sealed class ShotFiredMessage
{
    public int ShooterId { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public string Result { get; set; } = "";
    public int CurrentTurnPlayerId { get; set; }
    public string Phase { get; set; } = "";
    public bool ForcedTimeout { get; set; }
    public DateTimeOffset ShotClockDeadlineUtc { get; set; }
}

public sealed class PlacementPhaseStartedMessage
{
    public DateTimeOffset DeadlineUtc { get; set; }
    public int Seconds { get; set; }
    public int ShipsPerPlayer { get; set; }
}

public sealed class BattleStartedMessage
{
    public int Player1Id { get; set; }
    public int Player2Id { get; set; }
    public int CurrentTurnPlayerId { get; set; }
    public string Phase { get; set; } = "";
}

public sealed class MatchEndedMessage
{
    public int WinnerUserId { get; set; }
    public int Player1Id { get; set; }
    public int Player2Id { get; set; }
    public string? Player1UserName { get; set; }
    public string? Player2UserName { get; set; }
    public int Player1RatingChange { get; set; }
    public int Player2RatingChange { get; set; }
    public DateTimeOffset PostMatchDeadlineUtc { get; set; }
    public string? EndReason { get; set; }
}

public sealed class ResumableMatchResponseDto
{
    public int MatchId { get; set; }
}

/// <summary>
/// Returned when a match is already finished in the database (e.g. UI server restarted while API kept running).
/// Lets the client show the post-match screen without relying on a missed SignalR <c>MatchEnded</c>.
/// </summary>
public sealed class MatchRecoveryStateDto
{
    public int MatchId { get; set; }

    /// <summary>Match status enum value (same as domain <c>MatchStatus</c>).</summary>
    public int Status { get; set; }

    public int Player1Id { get; set; }
    public int Player2Id { get; set; }

    public string? Player1UserName { get; set; }
    public string? Player2UserName { get; set; }

    /// <summary>Null when <see cref="Status"/> is <c>Abandoned</c> with no winner.</summary>
    public int? WinnerUserId { get; set; }

    public int Player1RatingChange { get; set; }
    public int Player2RatingChange { get; set; }

    public int TotalNoOfMoves { get; set; }

    public string? FinishReason { get; set; }
}

public sealed class FleetLayoutCellDto
{
    public int X { get; set; }
    public int Y { get; set; }
    public int ShipTypeId { get; set; }
}

public sealed class ShotIntelDto
{
    public int X { get; set; }
    public int Y { get; set; }
    public string Result { get; set; } = "";
}

public sealed class FleetPlacementSyncMessage
{
    public List<FleetLayoutCellDto>? Cells { get; set; }

    /// <summary>Authoritative rectangles for this player (same as hub draft/finalize).</summary>
    public List<PlacedShipDto>? Placements { get; set; }
}

public sealed class YourBattleSnapshotMessage
{
    public int Player1Id { get; set; }
    public int Player2Id { get; set; }
    public int CurrentTurnPlayerId { get; set; }
    public string Phase { get; set; } = "";
    public List<FleetLayoutCellDto>? OwnFleet { get; set; }
    public List<ShotIntelDto>? MyOutgoing { get; set; }
    public List<ShotIntelDto>? IncomingOnOwn { get; set; }
    public DateTimeOffset ShotClockDeadlineUtc { get; set; }
}