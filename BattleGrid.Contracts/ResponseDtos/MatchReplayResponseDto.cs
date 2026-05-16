namespace BattleGrid.Contracts.ResponseDtos;

public sealed class MatchReplayResponseDto
{
    public int MatchId { get; init; }
    public int Player1Id { get; init; }
    public int Player2Id { get; init; }
    public string Player1UserName { get; init; } = string.Empty;
    public string Player2UserName { get; init; } = string.Empty;
    public IReadOnlyList<MatchReplayPlacementDto> Placements { get; init; } = Array.Empty<MatchReplayPlacementDto>();
    public IReadOnlyList<MatchReplayMoveDto> Moves { get; init; } = Array.Empty<MatchReplayMoveDto>();
}

public sealed class MatchReplayPlacementDto
{
    public int PlayerId { get; init; }
    public int ShipId { get; init; }
    public int StartX { get; init; }
    public int StartY { get; init; }
    public bool IsVertical { get; init; }
    public int Length { get; init; }
    public int Width { get; init; }
}

public sealed class MatchReplayMoveDto
{
    public int MoveNumber { get; init; }
    public int ShooterPlayerId { get; init; }
    public int HitX { get; init; }
    public int HitY { get; init; }
    public bool IsHit { get; init; }
}