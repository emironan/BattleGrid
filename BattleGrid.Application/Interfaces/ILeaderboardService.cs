using BattleGrid.Contracts.ResponseDtos;

namespace BattleGrid.Application.Interfaces;

public interface ILeaderboardService
{
    Task<IEnumerable<LeaderboardEntryDto>> GetCurrentSeasonLeaderboardAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<LeaderboardEntryDto>> GetAllTimeLeaderboardAsync(CancellationToken cancellationToken = default);
}