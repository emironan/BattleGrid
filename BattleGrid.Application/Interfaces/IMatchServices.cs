using BattleGrid.Contracts.RequestDtos;
using BattleGrid.Contracts.ResponseDtos;

namespace BattleGrid.Application.Interfaces
{
    public interface IMatchServices
    {
        Task<List<int>> GetPlayersAsync(int matchId);
        Task<GeneralResponseDto> ValidatePlayerAsync(int matchId, int playerId);
    }
}