using BattleGrid.Application.Interfaces;
using BattleGrid.Contracts.ResponseDtos;
using BattleGrid.Domain;
using BattleGrid.Domain.Entities;
using BattleGrid.Domain.Enums;
using BattleGrid.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BattleGrid.Application.Services
{
    public class MatchServices : IMatchServices
    {
        private readonly BattleGridDbContext _context;
        private readonly IPlayerStatSeasonService _playerStatSeason;
        private readonly IOptions<RatingFormulaSettings> _ratingFormula;

        public MatchServices(
            BattleGridDbContext context,
            IPlayerStatSeasonService playerStatSeason,
            IOptions<RatingFormulaSettings> ratingFormula)
        {
            _context = context;
            _playerStatSeason = playerStatSeason;
            _ratingFormula = ratingFormula;
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

        public async Task<PersistCompletedBattleResponseDto> PersistCompletedBattleAsync(
            int matchId,
            int winnerUserId,
            IReadOnlyList<(int shooterId, int moveNumber, int hitX, int hitY, bool hit)> moves,
            string? finishReason = null)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var match = await _context.Match.FirstOrDefaultAsync(m => m.MatchID == matchId);
                if (match is null)
                {
                    await tx.RollbackAsync();
                    return new PersistCompletedBattleResponseDto { Success = false, Message = "Match not found." };
                }

                if (match.Status == MatchStatus.P1Won || match.Status == MatchStatus.P2Won || match.Status == MatchStatus.Abandoned)
                {
                    await tx.CommitAsync();
                    return new PersistCompletedBattleResponseDto
                    {
                        Success = true,
                        Message = "Match already finalized.",
                        Player1RatingChange = match.P1RatingChange ?? 0,
                        Player2RatingChange = match.P2RatingChange ?? 0
                    };
                }

                if (winnerUserId != match.Player1ID && winnerUserId != match.Player2ID)
                {
                    await tx.RollbackAsync();
                    return new PersistCompletedBattleResponseDto { Success = false, Message = "Winner is not a player in this match." };
                }

                var p1Season = await _playerStatSeason.EnsureMatchmakingRatingAsync(match.Player1ID);
                var p2Season = await _playerStatSeason.EnsureMatchmakingRatingAsync(match.Player2ID);
                var r1 = PlayerRatingBounds.ClampRating(p1Season.RatingForQueue);
                var r2 = PlayerRatingBounds.ClampRating(p2Season.RatingForQueue);
                var p1Won = winnerUserId == match.Player1ID;
                var (p1Delta, p2Delta) = MatchRatingFormula.ComputeDeltasForPlayers(
                    r1,
                    r2,
                    p1Won,
                    _ratingFormula.Value);

                match.Status = p1Won ? MatchStatus.P1Won : MatchStatus.P2Won;
                match.FinishedAt = DateTimeOffset.UtcNow.ToUniversalTime();
                match.TotalNoOfMoves = moves.Count;
                match.P1RatingChange = p1Delta;
                match.P2RatingChange = p2Delta;
                if (!string.IsNullOrWhiteSpace(finishReason))
                    match.FinishReason = finishReason.Trim();

                foreach (var m in moves)
                {
                    _context.MatchMove.Add(new MatchMove
                    {
                        PlayerID = m.shooterId,
                        MatchID = matchId,
                        MoveNumber = m.moveNumber,
                        HitX = m.hitX,
                        HitY = m.hitY,
                        IsHit = m.hit
                    });
                }

                await UpsertPlayerStatAsync(match.Player1ID, p1Won, p1Delta);
                await UpsertPlayerStatAsync(match.Player2ID, !p1Won, p2Delta);

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return new PersistCompletedBattleResponseDto
                {
                    Success = true,
                    Message = "Match finalized.",
                    Player1RatingChange = p1Delta,
                    Player2RatingChange = p2Delta
                };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return new PersistCompletedBattleResponseDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<int?> GetResumableMatchIdAsync(int userId)
        {
            var id = await _context.Match
                .AsNoTracking()
                .Where(m => (m.Player1ID == userId || m.Player2ID == userId)
                            // If the match status is in progress, placing ships, or loading, it is resumable
                            && (m.Status == MatchStatus.InProgress
                                || m.Status == MatchStatus.PlacingShips
                                || m.Status == MatchStatus.Loading)
                            // If the match has started in the last 60 minutes, it is resumable
                            // Under normal circumtances it is impossible for a game to take more than 60 minutes to finish
                            && m.StartedAt > DateTimeOffset.UtcNow.AddMinutes(-60))
                .OrderByDescending(m => m.StartedAt)
                .Select(m => (int?)m.MatchID)
                .FirstOrDefaultAsync();

            return id;
        }

        public async Task<MatchRecoveryStateDto?> GetMatchRecoveryStateIfTerminalAsync(int matchId, int userId)
        {
            var row = await _context.Match
                .AsNoTracking()
                .Where(m => m.MatchID == matchId && (m.Player1ID == userId || m.Player2ID == userId))
                .Select(m => new
                {
                    m.MatchID,
                    m.Status,
                    m.Player1ID,
                    m.Player2ID,
                    m.P1RatingChange,
                    m.P2RatingChange,
                    m.TotalNoOfMoves,
                    m.FinishReason
                })
                .FirstOrDefaultAsync();

            if (row is null)
                return null;

            if (row.Status == MatchStatus.InProgress
                || row.Status == MatchStatus.PlacingShips
                || row.Status == MatchStatus.Loading)
                return null;

            int? winner = row.Status switch
            {
                MatchStatus.P1Won => row.Player1ID,
                MatchStatus.P2Won => row.Player2ID,
                MatchStatus.Abandoned => null,
                _ => null
            };

            var users = await _context.User.AsNoTracking()
                .Where(u => u.UserID == row.Player1ID || u.UserID == row.Player2ID)
                .Select(u => new { u.UserID, u.UserName })
                .ToListAsync();

            var p1Row = users.FirstOrDefault(u => u.UserID == row.Player1ID);
            var p2Row = users.FirstOrDefault(u => u.UserID == row.Player2ID);

            return new MatchRecoveryStateDto
            {
                MatchId = row.MatchID,
                Status = (int)row.Status,
                Player1Id = row.Player1ID,
                Player2Id = row.Player2ID,
                Player1UserName = FormatUserDisplayLabel(p1Row?.UserName ?? string.Empty, row.Player1ID),
                Player2UserName = FormatUserDisplayLabel(p2Row?.UserName ?? string.Empty, row.Player2ID),
                WinnerUserId = winner,
                Player1RatingChange = row.P1RatingChange ?? 0,
                Player2RatingChange = row.P2RatingChange ?? 0,
                TotalNoOfMoves = row.TotalNoOfMoves,
                FinishReason = row.FinishReason
            };
        }

        private static string FormatUserDisplayLabel(string userName, int userId)
        {
            if (!string.IsNullOrWhiteSpace(userName))
                return userName;
            return $"user #{userId}";
        }

        public async Task<GeneralResponseDto> PersistAbandonedMatchAsync(int matchId, string finishReason)
        {
            var row = await _context.Match.FirstOrDefaultAsync(m => m.MatchID == matchId);
            if (row is null)
                return new GeneralResponseDto { Success = false, Message = "Match not found." };

            if (row.Status == MatchStatus.P1Won || row.Status == MatchStatus.P2Won || row.Status == MatchStatus.Abandoned)
                return new GeneralResponseDto { Success = true, Message = "Match already closed." };

            row.Status = MatchStatus.Abandoned;
            row.FinishedAt = DateTimeOffset.UtcNow.ToUniversalTime();
            row.FinishReason = string.IsNullOrWhiteSpace(finishReason) ? null : finishReason.Trim();
            await _context.SaveChangesAsync();
            return new GeneralResponseDto { Success = true, Message = "Match abandoned." };
        }

        /// <summary> Stored on <see cref="Match.FinishReason"/> when the cleanup worker abandons long-running non-terminal matches. </summary>
        internal const string StaleMatchAutoAbandonFinishReason =
            "Automatically abandoned: the match remained in Loading, PlacingShips, or InProgress past the stale-match time limit without a decisive outcome. "
            + "Typical causes: both players disconnected, extended server outage, or an abandoned session.";

        public async Task<int> AbandonStaleMatchesAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
        {
            var cutoff = DateTimeOffset.UtcNow.ToUniversalTime() - olderThan;
            var activeStatuses = new[] { MatchStatus.Loading, MatchStatus.PlacingShips, MatchStatus.InProgress };

            var matchIds = await _context.Match.AsNoTracking()
                .Where(m => activeStatuses.Contains(m.Status) && m.StartedAt <= cutoff)
                .Select(m => m.MatchID)
                .ToListAsync(cancellationToken);

            var abandonedCount = 0;
            foreach (var matchId in matchIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = await PersistAbandonedMatchAsync(matchId, StaleMatchAutoAbandonFinishReason);
                if (result is { Success: true, Message: "Match abandoned." })
                    abandonedCount++;
            }

            return abandonedCount;
        }

        private async Task UpsertPlayerStatAsync(int userId, bool won, int ratingDelta)
        {
            var state = await _playerStatSeason.EnsureMatchmakingRatingAsync(userId);
            var season = state.GlobalCurrentSeason;

            var row = await _context.PlayerStat
                .SingleAsync(x => x.UserID == userId && x.SeasonNo == season);

            row.MatchesPlayed++;
            if (won)
                row.MatchesWon++;
            row.Rating = PlayerRatingBounds.ClampRating(row.Rating + ratingDelta);
            row.HighestRating = PlayerRatingBounds.ClampHighestAfterCurrent(row.HighestRating, row.Rating);
            row.LastUpdatedAt = DateTimeOffset.UtcNow.ToUniversalTime();
        }
    }
}