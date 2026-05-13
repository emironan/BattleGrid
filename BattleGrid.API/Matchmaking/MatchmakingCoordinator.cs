using BattleGrid.Application.Interfaces;
using BattleGrid.Domain.Entities;
using BattleGrid.Domain.Enums;
using BattleGrid.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using BattleGrid.API.Hubs;

namespace BattleGrid.API.Matchmaking;

public sealed class MatchmakingCoordinator
{
    private static readonly TimeSpan WindowStep = TimeSpan.FromSeconds(30);
    private const int InitialHalfWindow = 200;
    private const int WindowGrowthPerStep = 200;
    private const int MaxHalfWindow = 1000;

    private readonly IHubContext<QueueHub> _queueHub;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly List<WaitingEntry> _waiting = new();
    private readonly object _lock = new();

    public MatchmakingCoordinator(IHubContext<QueueHub> queueHub, IServiceScopeFactory scopeFactory)
    {
        _queueHub = queueHub;
        _scopeFactory = scopeFactory;
    }

    public async Task OnQueueConnectedAsync(string connectionId, int userId, CancellationToken cancellationToken = default)
    {
        int rating;
        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var seasonSvc = scope.ServiceProvider.GetRequiredService<IPlayerStatSeasonService>();
            var state = await seasonSvc.EnsureMatchmakingRatingAsync(userId, cancellationToken);
            rating = state.RatingForQueue;
        }

        lock (_lock)
        {
            _waiting.RemoveAll(w => w.ConnectionId == connectionId || w.UserId == userId);
            _waiting.Add(new WaitingEntry(connectionId, userId, DateTimeOffset.UtcNow, rating));
        }

        await TryMatchWaitingPlayersAsync(cancellationToken);
    }

    /// <summary>
    /// Attempts to form matches for all compatible pairs in the queue (greedy, oldest waiters first).
    /// </summary>
    public async Task TryMatchWaitingPlayersAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            var now = DateTimeOffset.UtcNow;
            WaitingEntry? first = null;
            WaitingEntry? second = null;

            lock (_lock)
            {
                var ordered = _waiting
                    .OrderBy(w => w.JoinedAtUtc)
                    .ThenBy(w => w.UserId)
                    .ToList();

                for (var i = 0; i < ordered.Count && first is null; i++)
                {
                    for (var j = i + 1; j < ordered.Count; j++)
                    {
                        if (RatingsMutuallyAcceptable(ordered[i], ordered[j], now))
                        {
                            first = ordered[i];
                            second = ordered[j];
                            _waiting.RemoveAll(w =>
                                w.ConnectionId == first.ConnectionId || w.ConnectionId == second.ConnectionId);
                            break;
                        }
                    }
                }
            }

            if (first is null || second is null)
                return;

            var earlier = first.JoinedAtUtc <= second.JoinedAtUtc ? first : second;
            var later = ReferenceEquals(earlier, first) ? second : first;

            int matchId;
            await using (var scope = _scopeFactory.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<BattleGridDbContext>();
                var match = new Match
                {
                    Player1ID = earlier.UserId,
                    Player2ID = later.UserId,
                    Status = MatchStatus.Loading,
                    StartedAt = DateTimeOffset.UtcNow
                };
                db.Match.Add(match);
                await db.SaveChangesAsync(cancellationToken);
                matchId = match.MatchID;
            }

            await _queueHub.Clients.Client(earlier.ConnectionId)
                .SendAsync("Matched", matchId, later.UserId, cancellationToken);
            await _queueHub.Clients.Client(later.ConnectionId)
                .SendAsync("Matched", matchId, earlier.UserId, cancellationToken);
        }
    }

    public void OnQueueDisconnected(string connectionId)
    {
        lock (_lock)
        {
            _waiting.RemoveAll(w => w.ConnectionId == connectionId);
        }
    }

    /// <summary>
    /// Half-width of the rating window: ±value around the player's rating.
    /// First 30s: 200, then +200 each 30s, capped at 1000.
    /// </summary>
    internal static int GetRatingHalfWindow(TimeSpan elapsedInQueue)
    {
        if (elapsedInQueue < TimeSpan.Zero)
            elapsedInQueue = TimeSpan.Zero;

        var periods = (int)Math.Floor(elapsedInQueue.TotalSeconds / WindowStep.TotalSeconds);
        var half = InitialHalfWindow + periods * WindowGrowthPerStep;
        return Math.Min(MaxHalfWindow, half);
    }

    internal static bool RatingsMutuallyAcceptable(WaitingEntry a, WaitingEntry b, DateTimeOffset now)
    {
        var ra = Math.Max(1, a.Rating);
        var rb = Math.Max(1, b.Rating);
        var diff = Math.Abs(ra - rb);
        var wa = GetRatingHalfWindow(now - a.JoinedAtUtc);
        var wb = GetRatingHalfWindow(now - b.JoinedAtUtc);
        return diff <= wa && diff <= wb;
    }

    internal sealed record WaitingEntry(
        string ConnectionId,
        int UserId,
        DateTimeOffset JoinedAtUtc,
        int Rating);
}
