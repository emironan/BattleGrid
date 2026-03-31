namespace BattleGrid.Domain.Enums;

public enum GamePhase
{
    WaitingForPlayers,
    ShipPlacement,
    InProgress,
    P1Won,
    P2Won,
    Abandoned
}