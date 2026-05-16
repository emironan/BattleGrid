namespace BattleGrid.Application.Interfaces;

public interface IBanListServices
{
    /// <summary>
    /// Marks elapsed temporary BanList rows as reverted and clears User.IsBanned
    /// when no active ban remains. Returns the number of ban rows reverted.
    /// </summary>
    Task<int> ExpireElapsedTemporaryBansAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-evaluates ban state for one player (e.g. at login). Returns true when the player is not banned.
    /// </summary>
    Task<bool> RefreshPlayerBanStateAsync(int playerId, CancellationToken cancellationToken = default);
}
