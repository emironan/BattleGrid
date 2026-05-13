using BattleGrid.Application.Interfaces;
using BattleGrid.Contracts.ResponseDtos;
using BattleGrid.Domain.Entities;
using BattleGrid.Domain.Enums;
using BattleGrid.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BattleGrid.Application.Services
{
    public class MatchServices : IMatchServices
    {
        private readonly BattleGridDbContext _context;
        private readonly IPlayerStatSeasonService _playerStatSeason;

        public MatchServices(BattleGridDbContext context, IPlayerStatSeasonService playerStatSeason)
        {
            _context = context;
            _playerStatSeason = playerStatSeason;
        }

        public async Task<List<int>> GetPlayersAsync(int matchId)
        {
            var row = await _context.Match
                .AsNoTracking()
                .Where(m => m.MatchID == matchId)
                .Select(m => new { m.Player1ID, m.Player2ID })
                .FirstOrDefaultAsync();

            if (row is null)
                return new List<int>();

            return new List<int> { row.Player1ID, row.Player2ID };
        }

        public async Task<GeneralResponseDto> ValidatePlayerAsync(int matchId, int playerId)
        {
            var match = await _context.Match
                .Where(m => m.MatchID == matchId)
                .AsNoTracking()
                .AnyAsync();

            // If the given matchId does not exist in DB
            if (!match)
            {
                return new GeneralResponseDto
                {
                    Success = false,
                    Message = "Match not found!"
                };
            }

            var playedInMatch = await _context.Match
                .Where(m => m.MatchID == matchId && (m.Player1ID == playerId || m.Player2ID == playerId))
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if(playedInMatch == null)
            {
                return new GeneralResponseDto
                {
                    Success = false,
                    Message = "Requesting user is not a player in this match."
                };
            }
            return new GeneralResponseDto
            {
                Success = true,
                Message = "User is a player in this match."
            };
        }

        public async Task<GeneralResponseDto> TrySetMatchStatusAsync(int matchId, MatchStatus status)
        {
            var row = await _context.Match.FirstOrDefaultAsync(m => m.MatchID == matchId);
            if (row is null)
            {
                return new GeneralResponseDto { Success = false, Message = "Match not found." };
            }

            row.Status = status;
            await _context.SaveChangesAsync();
            return new GeneralResponseDto { Success = true, Message = "Match updated." };
        }

        public async Task<GeneralResponseDto> PersistCompletedBattleAsync(
            int matchId,
            int winnerUserId,
            IReadOnlyList<(int shooterId, int moveNumber, int hitX, int hitY, bool hit)> moves)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var match = await _context.Match.FirstOrDefaultAsync(m => m.MatchID == matchId);
                if (match is null)
                {
                    await tx.RollbackAsync();
                    return new GeneralResponseDto { Success = false, Message = "Match not found." };
                }

                if (match.Status == MatchStatus.P1Won || match.Status == MatchStatus.P2Won)
                {
                    await tx.CommitAsync();
                    return new GeneralResponseDto { Success = true, Message = "Match already finalized." };
                }

                if (winnerUserId != match.Player1ID && winnerUserId != match.Player2ID)
                {
                    await tx.RollbackAsync();
                    return new GeneralResponseDto { Success = false, Message = "Winner is not a player in this match." };
                }

                const int delta = 15;
                var p1Won = winnerUserId == match.Player1ID;
                var p1Delta = p1Won ? delta : -delta;
                var p2Delta = p1Won ? -delta : delta;

                match.Status = p1Won ? MatchStatus.P1Won : MatchStatus.P2Won;
                match.FinishedAt = DateTimeOffset.UtcNow;
                match.TotalNoOfTurns = moves.Count;
                match.P1RatingChange = p1Delta;
                match.P2RatingChange = p2Delta;

                foreach (var m in moves)
                {
                    _context.MatchMove.Add(new MatchMove
                    {
                        PlayerID = m.shooterId,
                        MatchID = matchId,
                        MoveNumber = m.moveNumber,
                        HitX = m.hitX,
                        HitY = m.hitY,
                        Result = m.hit
                    });
                }

                await UpsertPlayerStatAsync(match.Player1ID, p1Won, p1Delta);
                await UpsertPlayerStatAsync(match.Player2ID, !p1Won, p2Delta);

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return new GeneralResponseDto { Success = true, Message = "Match finalized." };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return new GeneralResponseDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<int?> GetResumableMatchIdAsync(int userId)
        {
            var id = await _context.Match.AsNoTracking()
                .Where(m => (m.Player1ID == userId || m.Player2ID == userId)
                            && (m.Status == MatchStatus.InProgress
                                || m.Status == MatchStatus.PlacingShips
                                || m.Status == MatchStatus.Loading))
                .OrderByDescending(m => m.StartedAt)
                .Select(m => (int?)m.MatchID)
                .FirstOrDefaultAsync();

            return id;
        }

        public async Task<GeneralResponseDto> PersistAbandonedMatchAsync(int matchId, string finishReason)
        {
            var row = await _context.Match.FirstOrDefaultAsync(m => m.MatchID == matchId);
            if (row is null)
                return new GeneralResponseDto { Success = false, Message = "Match not found." };

            if (row.Status == MatchStatus.P1Won || row.Status == MatchStatus.P2Won || row.Status == MatchStatus.Abandoned)
                return new GeneralResponseDto { Success = true, Message = "Match already closed." };

            row.Status = MatchStatus.Abandoned;
            row.FinishedAt = DateTimeOffset.UtcNow;
            row.FinishReason = finishReason;
            await _context.SaveChangesAsync();
            return new GeneralResponseDto { Success = true, Message = "Match abandoned." };
        }

        private async Task UpsertPlayerStatAsync(int userId, bool won, int ratingDelta)
        {
            var state = await _playerStatSeason.EnsureMatchmakingRatingAsync(userId);
            var season = state.GlobalCurrentSeason;

            var row = await _context.PlayerStat
                .Where(x => x.UserID == userId && x.SeasonNo == season)
                .OrderByDescending(x => x.StatID)
                .FirstOrDefaultAsync();

            if (row is null)
            {
                var rating = Math.Max(100, 1000 + ratingDelta);
                _context.PlayerStat.Add(new PlayerStat
                {
                    UserID = userId,
                    SeasonNo = season,
                    MatchesPlayed = 1,
                    MatchesWon = won ? 1 : 0,
                    Rating = rating,
                    HighestRating = Math.Max(1000, rating),
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastUpdatedAt = DateTimeOffset.UtcNow
                });
                return;
            }

            row.MatchesPlayed++;
            if (won)
                row.MatchesWon++;
            row.Rating = Math.Max(100, row.Rating + ratingDelta);
            if (row.Rating > row.HighestRating)
                row.HighestRating = row.Rating;
            row.LastUpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}