using BattleGrid.Contracts.ResponseDtos;

namespace BattleGrid.Application.Interfaces
{
    public interface IReplayServices
    {
        /// <summary>Latest terminal matches for <paramref name="userId"/>, newest first.</summary>
        Task<MatchHistoryResponseDto> GetRecentMatchHistoryAsync(int userId, int limit = 20);

        /// <summary>Ship placements and ordered moves for replay when <paramref name="userId"/> played in a won match.</summary>
        Task<MatchReplayResponseDto?> GetMatchReplayAsync(int matchId, int userId);

        /// <summary>Replay for any finished won match (admin moderation; no participant check).</summary>
        Task<MatchReplayResponseDto?> GetMatchReplayForAdminAsync(int matchId);
    }
}
