using BattleGrid.Contracts.RequestDtos;
using BattleGrid.Contracts.ResponseDtos;
using BattleGrid.Domain.Enums;

namespace BattleGrid.Application.Interfaces
{
    public interface IMatchServices
    {
        Task<List<int>> GetPlayersAsync(int matchId);
        Task<GeneralResponseDto> ValidatePlayerAsync(int matchId, int playerId);
        Task<GeneralResponseDto> TrySetMatchStatusAsync(int matchId, MatchStatus status);

        /// <summary> Persists match outcome, rating changes, and all recorded shots (hits/misses only — not duplicate-target attempts). </summary>
        Task<GeneralResponseDto> PersistCompletedBattleAsync(
            int matchId,
            int winnerUserId,
            IReadOnlyList<(int shooterId, int moveNumber, int hitX, int hitY, bool hit)> moves);

        Task<int?> GetResumableMatchIdAsync(int userId);

        Task<GeneralResponseDto> PersistAbandonedMatchAsync(int matchId, string finishReason);
    }
}