using BattleGrid.Contracts.RequestDtos;
using BattleGrid.Contracts.ResponseDtos;

namespace BattleGrid.Application.Interfaces
{
    public interface IMatchServices
    {
        Task<List<int>> GetPlayersAsync(int matchId);
        Task<bool> ValidatePlayerAsync(int matchId, int playerId);
    }
}