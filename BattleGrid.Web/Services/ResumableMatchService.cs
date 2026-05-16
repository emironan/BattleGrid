namespace BattleGrid.Web.Services;

/// <summary>
/// Tracks whether the signed-in user has an active match they should rejoin instead of queueing.
/// </summary>
public sealed class ResumableMatchService
{
    private readonly ApiService _api;
    private readonly AuthStateService _auth;

    public ResumableMatchService(ApiService api, AuthStateService auth)
    {
        _api = api;
        _auth = auth;
        _auth.OnChange += OnAuthChanged;
    }

    public event Action? OnChange;

    public int? MatchId { get; private set; }

    public bool HasResumableMatch => MatchId is not null;

    public bool IsLoaded { get; private set; }

    public async Task RefreshAsync()
    {
        await _auth.InitializeAsync();
        if (!_auth.IsLoggedIn)
        {
            MatchId = null;
            IsLoaded = true;
            Notify();
            return;
        }

        var dto = await _api.GetResumableMatchAsync();
        MatchId = dto?.MatchId;
        IsLoaded = true;
        Notify();
    }

    void OnAuthChanged() => _ = RefreshAsync();

    void Notify() => OnChange?.Invoke();
}