using BattleGrid.Application.Interfaces;

namespace BattleGrid.API.BackgroundServices;

/// <summary>
/// Reverts elapsed temporary bans and clears stale <c>User.IsBanned</c> flags.
/// </summary>
public sealed class ExpiredBanCleanupBackgroundService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(1);
    private readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(1);    // Background service starts 1 min after the server starts running

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredBanCleanupBackgroundService> _logger;

    public ExpiredBanCleanupBackgroundService(IServiceScopeFactory scopeFactory,
                                              ILogger<ExpiredBanCleanupBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TickInterval);

        // Initial delay to allow the application to fully start
        await Task.Delay(StartupDelay, stoppingToken);

        try
        {
            await RunCleanupAsync(stoppingToken);
            while (!stoppingToken.IsCancellationRequested &&
                   await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await RunCleanupAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Expired ban cleanup tick failed.");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutdown
        }
    }

    async Task RunCleanupAsync(CancellationToken stoppingToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var banList = scope.ServiceProvider.GetRequiredService<IBanListServices>();
        var reverted = await banList.ExpireElapsedTemporaryBansAsync(stoppingToken);
        if (reverted > 0)
        {
            _logger.LogInformation(
                "Expired ban cleanup reverted {Count} temporary ban(s).",
                reverted);
        }
    }
}
