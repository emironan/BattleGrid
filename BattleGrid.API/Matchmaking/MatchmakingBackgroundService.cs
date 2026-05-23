namespace BattleGrid.API.Matchmaking;

/// <summary>
/// Periodically runs matchmaking so pairs that become compatible as windows widen are matched without reconnecting.
/// </summary>
public sealed class MatchmakingBackgroundService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(5);    // Queue checks for suitable opponents every 5 seconds

    private readonly MatchmakingCoordinator _coordinator;
    private readonly ILogger<MatchmakingBackgroundService> _logger;

    public MatchmakingBackgroundService(
        MatchmakingCoordinator coordinator,
        ILogger<MatchmakingBackgroundService> logger)
    {
        _coordinator = coordinator;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TickInterval);
        try
        {
            await _coordinator.TryMatchWaitingPlayersAsync(stoppingToken);
            while (!stoppingToken.IsCancellationRequested &&
                   await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await _coordinator.TryMatchWaitingPlayersAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Matchmaking background tick failed.");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutdown
        }
    }
}