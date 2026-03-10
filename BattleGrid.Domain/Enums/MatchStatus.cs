namespace BattleGrid.Domain.Enums;

public enum MatchStatus
{
    Abandoned = 0,
    P1Won = 1,
    P2Won = 2,
    InProgress = 3,
    PlacingShips = 4,
    Loading = 5
}