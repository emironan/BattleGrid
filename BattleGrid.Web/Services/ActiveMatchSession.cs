namespace BattleGrid.Web.Services;

/// <summary>Tracks an active /game/{id} session so the shell can hide global navigation and block stray routing.</summary>
public sealed class ActiveMatchSession
{
    int? _matchId;

    public int? MatchId => _matchId;

    public bool IsLocked => _matchId is not null;

    public event Action? OnChange;

    public void EnterMatch(int matchId)
    {
        _matchId = matchId;
        OnChange?.Invoke();
    }

    public void LeaveMatch()
    {
        _matchId = null;
        OnChange?.Invoke();
    }
}
