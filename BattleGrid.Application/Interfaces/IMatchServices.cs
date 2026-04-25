using BattleGrid.Contracts.RequestDtos;
using BattleGrid.Contracts.ResponseDtos;

namespace BattleGrid.Application.Interfaces
{
    public interface IMatchServices
    {
        Task<List<int>> GetPlayersAsync(int matchId);
        Task<bool> ValidatePlayer(int matchId, int playerId);
    }
}