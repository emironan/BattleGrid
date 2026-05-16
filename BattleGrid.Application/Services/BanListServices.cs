using BattleGrid.Application.Interfaces;
using BattleGrid.Domain.Entities;
using BattleGrid.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BattleGrid.Application.Services;

public sealed class BanListServices : IBanListServices
{
    public const string AutoRevertReason = "Ban duration expired (automatic system revert).";

    private readonly BattleGridDbContext _context;

    public BanListServices(BattleGridDbContext context)
    {
        _context = context;
    }

    public async Task<int> ExpireElapsedTemporaryBansAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow.ToUniversalTime();

        var openTemporaryBans = await _context.BanList
            .Where(b => b.IsTemporary && !b.IsReverted)
            .ToListAsync(cancellationToken);

        var expired = openTemporaryBans.Where(b => IsExpired(b, now)).ToList();
        foreach (var ban in expired)
            MarkReverted(ban);

        var affectedPlayerIds = expired.Select(b => b.PlayerID).Distinct().ToList();
        if (affectedPlayerIds.Count > 0)
            await ClearUserBanFlagsWhereNoActiveBanAsync(affectedPlayerIds, now, cancellationToken);

        var revertedCount = expired.Count;
        if (revertedCount > 0)
            await _context.SaveChangesAsync(cancellationToken);

        await SyncStaleUserBanFlagsAsync(now, cancellationToken);

        return revertedCount;
    }

    public async Task<bool> RefreshPlayerBanStateAsync(int playerId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow.ToUniversalTime();

        var openTemporaryBans = await _context.BanList
            .Where(b => b.PlayerID == playerId && b.IsTemporary && !b.IsReverted)
            .ToListAsync(cancellationToken);

        var changed = false;
        foreach (var ban in openTemporaryBans.Where(b => IsExpired(b, now)))
        {
            MarkReverted(ban);
            changed = true;
        }

        var user = await _context.User.FirstOrDefaultAsync(u => u.UserID == playerId, cancellationToken);
        if (user is null)
            return true;

        if (!user.IsBanned && !changed)
            return true;

        var hasActiveBan = await PlayerHasActiveBanAsync(playerId, now, cancellationToken);
        if (user.IsBanned && !hasActiveBan)
        {
            user.IsBanned = false;
            user.UpdateReason = "Ban cleared (automatic sync).";
            user.LastUpdatedAt = now;
            changed = true;
        }

        if (changed)
            await _context.SaveChangesAsync(cancellationToken);

        return !user.IsBanned;
    }

    private async Task<int> SyncStaleUserBanFlagsAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var bannedUserIds = await _context.User
            .AsNoTracking()
            .Where(u => u.IsBanned)
            .Select(u => u.UserID)
            .ToListAsync(cancellationToken);

        if (bannedUserIds.Count == 0)
            return 0;

        var cleared = 0;
        foreach (var playerId in bannedUserIds)
        {
            if (await PlayerHasActiveBanAsync(playerId, now, cancellationToken))
                continue;

            var user = await _context.User.FirstOrDefaultAsync(u => u.UserID == playerId, cancellationToken);
            if (user is null || !user.IsBanned)
                continue;

            user.IsBanned = false;
            user.UpdateReason = "Ban cleared (automatic sync).";
            user.LastUpdatedAt = now;
            cleared++;
        }

        if (cleared > 0)
            await _context.SaveChangesAsync(cancellationToken);

        return cleared;
    }

    private async Task ClearUserBanFlagsWhereNoActiveBanAsync(
        IReadOnlyList<int> playerIds,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        foreach (var playerId in playerIds)
        {
            if (await PlayerHasActiveBanAsync(playerId, now, cancellationToken))
                continue;

            var user = await _context.User.FirstOrDefaultAsync(u => u.UserID == playerId, cancellationToken);
            if (user is null || !user.IsBanned)
                continue;

            user.IsBanned = false;
            user.UpdateReason = "Temporary ban expired (automatic).";
            user.LastUpdatedAt = now;
        }
    }

    private async Task<bool> PlayerHasActiveBanAsync(int playerId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var latest = await _context.BanList
            .AsNoTracking()
            .Where(b => b.PlayerID == playerId)
            .OrderByDescending(b => b.BanID)
            .FirstOrDefaultAsync(cancellationToken);

        return latest is not null && IsBanActive(latest, now);
    }

    private static void MarkReverted(BanList ban)
    {
        ban.IsReverted = true;
        ban.RevertingAdminID = null;
        ban.RevertingReason = AutoRevertReason;
    }

    internal static bool IsBanActive(BanList ban, DateTimeOffset now)
    {
        if (ban.IsReverted)
            return false;

        if (!ban.IsTemporary)
            return true;

        var end = GetBanEndUtc(ban);
        return end.HasValue && end.Value > now;
    }

    internal static bool IsExpired(BanList ban, DateTimeOffset now)
    {
        if (!ban.IsTemporary || ban.IsReverted)
            return false;

        var end = GetBanEndUtc(ban);
        return end.HasValue && end.Value <= now;
    }

    internal static DateTimeOffset? GetBanEndUtc(BanList ban)
    {
        if (ban.BannedUntil.HasValue)
            return ban.BannedUntil.Value.ToUniversalTime();

        if (ban.Duration.HasValue)
            return ban.BannedAt.ToUniversalTime().Add(ban.Duration.Value);

        return null;
    }
}