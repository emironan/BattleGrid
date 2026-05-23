namespace BattleGrid.Web.GameVisuals;

// Explicit shot marker phase per cell (avoids timer races on render).
public enum ShotCellFxPhase
{
    MissAnimation,
    MissSettled,
    HitAnimation,
    HitSettled
}
