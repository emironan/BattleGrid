using System.Text.RegularExpressions;
using BattleGrid.Application.Interfaces;
using BattleGrid.Contracts.ResponseDtos;
using BattleGrid.Domain.Enums;
using BattleGrid.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BattleGrid.Application.Services;

public class ReplayServices : IReplayServices
{
    private static readonly MatchStatus[] ReplayableStatuses = { MatchStatus.P1Won, MatchStatus.P2Won };

    private readonly BattleGridDbContext _context;

    public ReplayServices(BattleGridDbContext context)
    {
        _context = context;
    }

    public async Task<MatchHistoryResponseDto> GetRecentMatchHistoryAsync(int userId, int limit = 20)
    {
        if (limit < 1)
            limit = 1;
        if (limit > 20)
            limit = 20;

        var finishedStatuses = new[] { MatchStatus.Abandoned, MatchStatus.P1Won, MatchStatus.P2Won };

        var rows = await _context.Match
            .AsNoTracking()
            .Where(m => (m.Player1ID == userId || m.Player2ID == userId) && finishedStatuses.Contains(m.Status))
            .OrderByDescending(m => m.FinishedAt ?? m.StartedAt)
            .Take(limit)
            .Select(m => new
            {
                m.MatchID,
                m.Status,
                m.Player1ID,
                m.Player2ID,
                m.P1RatingChange,
                m.P2RatingChange,
                m.FinishReason,
                m.FinishedAt,
                m.StartedAt
            })
            .ToListAsync();

        if (rows.Count == 0)
            return new MatchHistoryResponseDto();

        var opponentIds = rows
            .Select(m => m.Player1ID == userId ? m.Player2ID : m.Player1ID)
            .Distinct()
            .ToList();

        var opponentNames = await _context.User
            .AsNoTracking()
            .Where(u => opponentIds.Contains(u.UserID))
            .Select(u => new { u.UserID, u.UserName })
            .ToDictionaryAsync(u => u.UserID, u => u.UserName);

        var entries = rows.Select(m =>
        {
            var opponentId = m.Player1ID == userId ? m.Player2ID : m.Player1ID;
            opponentNames.TryGetValue(opponentId, out var opponentUserName);

            var ratingChange = m.Player1ID == userId
                ? m.P1RatingChange ?? 0
                : m.P2RatingChange ?? 0;

            bool? playerWon = m.Status switch
            {
                MatchStatus.P1Won => m.Player1ID == userId,
                MatchStatus.P2Won => m.Player2ID == userId,
                _ => null
            };

            return new MatchHistoryEntryResponseDto
            {
                MatchId = m.MatchID,
                OpponentUserName = FormatUserDisplayLabel(opponentUserName ?? string.Empty, opponentId),
                PlayerWon = playerWon,
                RatingChange = ratingChange,
                Status = (int)m.Status,
                EndKind = ClassifyMatchEnd(userId, m.Status, m.FinishReason),
                FinishedAt = m.FinishedAt ?? m.StartedAt
            };
        }).ToList();

        return new MatchHistoryResponseDto { Matches = entries };
    }

    public async Task<MatchReplayResponseDto?> GetMatchReplayAsync(int matchId, int userId)
    {
        var mayView = await _context.Match
            .AsNoTracking()
            .AnyAsync(m => m.MatchID == matchId
                           && (m.Player1ID == userId || m.Player2ID == userId)
                           && ReplayableStatuses.Contains(m.Status));

        if (!mayView)
            return null;

        return await LoadMatchReplayAsync(matchId);
    }

    public async Task<MatchReplayResponseDto?> GetMatchReplayForAdminAsync(int matchId)
    {
        var mayView = await _context.Match
            .AsNoTracking()
            .AnyAsync(m => m.MatchID == matchId && ReplayableStatuses.Contains(m.Status));

        if (!mayView)
            return null;

        return await LoadMatchReplayAsync(matchId);
    }

    private async Task<MatchReplayResponseDto?> LoadMatchReplayAsync(int matchId)
    {
        var match = await _context.Match
            .AsNoTracking()
            .Where(m => m.MatchID == matchId)
            .Select(m => new { m.MatchID, m.Player1ID, m.Player2ID, m.Status, m.FinishReason })
            .FirstOrDefaultAsync();

        if (match is null)
            return null;

        var users = await _context.User
            .AsNoTracking()
            .Where(u => u.UserID == match.Player1ID || u.UserID == match.Player2ID)
            .Select(u => new { u.UserID, u.UserName })
            .ToListAsync();

        var p1Row = users.FirstOrDefault(u => u.UserID == match.Player1ID);
        var p2Row = users.FirstOrDefault(u => u.UserID == match.Player2ID);

        var placementRows = await _context.ShipPlacement
            .AsNoTracking()
            .Where(p => p.MatchID == matchId)
            .OrderBy(p => p.PlacementID)
            .Select(p => new
            {
                p.PlayerID,
                p.ShipID,
                p.StartX,
                p.StartY,
                p.IsVertical
            })
            .ToListAsync();

        var shipTypeIds = placementRows.Select(p => p.ShipID).Distinct().ToList();
        var shipTypes = await _context.ShipType
            .AsNoTracking()
            .Where(s => shipTypeIds.Contains(s.ShipID))
            .Select(s => new { s.ShipID, s.Length, s.Width })
            .ToDictionaryAsync(s => s.ShipID, s => (s.Length, s.Width));

        var placements = placementRows.Select(p =>
        {
            shipTypes.TryGetValue(p.ShipID, out var dims);
            var length = dims.Length > 0 ? dims.Length : 1;
            var width = dims.Width > 0 ? dims.Width : 1;
            return new MatchReplayPlacementDto
            {
                PlayerId = p.PlayerID,
                ShipId = p.ShipID,
                StartX = p.StartX,
                StartY = p.StartY,
                IsVertical = p.IsVertical,
                Length = length,
                Width = width
            };
        }).ToList();

        var moves = await _context.MatchMove
            .AsNoTracking()
            .Where(m => m.MatchID == matchId)
            .OrderBy(m => m.MoveNumber)
            .ThenBy(m => m.MoveID)
            .Select(m => new MatchReplayMoveDto
            {
                MoveNumber = m.MoveNumber,
                ShooterPlayerId = m.PlayerID,
                HitX = m.HitX,
                HitY = m.HitY,
                IsHit = m.IsHit
            })
            .ToListAsync();

        var p1Name = FormatUserDisplayLabel(p1Row?.UserName ?? string.Empty, match.Player1ID);
        var p2Name = FormatUserDisplayLabel(p2Row?.UserName ?? string.Empty, match.Player2ID);
        var winnerUserId = match.Status switch
        {
            MatchStatus.P1Won => match.Player1ID,
            MatchStatus.P2Won => match.Player2ID,
            _ => (int?)null
        };

        return new MatchReplayResponseDto
        {
            MatchId = match.MatchID,
            Player1Id = match.Player1ID,
            Player2Id = match.Player2ID,
            Player1UserName = p1Name,
            Player2UserName = p2Name,
            WinnerUserId = winnerUserId,
            Status = (int)match.Status,
            FinishReason = match.FinishReason,
            ResultSummary = BuildReplayResultSummary(
                winnerUserId,
                match.FinishReason,
                match.Player1ID,
                match.Player2ID,
                p1Name,
                p2Name),
            Placements = placements,
            Moves = moves
        };
    }

    private static string BuildReplayResultSummary(
        int? winnerUserId,
        string? finishReason,
        int player1Id,
        int player2Id,
        string player1UserName,
        string player2UserName)
    {
        if (winnerUserId is not int winnerId)
            return "Match ended with no recorded winner.";

        string Label(int userId) =>
            userId == player1Id ? player1UserName : player2UserName;

        var winnerLabel = Label(winnerId);

        if (string.IsNullOrWhiteSpace(finishReason)
            || finishReason.Contains("ships were destroyed", StringComparison.OrdinalIgnoreCase))
        {
            return $"Winning shot - {winnerLabel} won";
        }

        var leavingUserId = TryParseLeavingUserId(finishReason);
        if (leavingUserId is int leaverId)
        {
            var leaverLabel = Label(leaverId);
            if (finishReason.Contains("abandoned ship", StringComparison.OrdinalIgnoreCase))
                return $"{leaverLabel} abandoned ship - {winnerLabel} won";
            if (finishReason.Contains("forfeited", StringComparison.OrdinalIgnoreCase))
                return $"{leaverLabel} forfeited - {winnerLabel} won";
            return $"{leaverLabel} left - {winnerLabel} won";
        }

        return $"{winnerLabel} won";
    }

    private static string FormatUserDisplayLabel(string userName, int userId)
    {
        if (!string.IsNullOrWhiteSpace(userName))
            return userName;
        return $"user #{userId}";
    }

    private static MatchHistoryEndKind ClassifyMatchEnd(int userId, MatchStatus status, string? finishReason)
    {
        if (status == MatchStatus.Abandoned)
            return MatchHistoryEndKind.Abandoned;

        if (string.IsNullOrWhiteSpace(finishReason)
            || finishReason.Contains("ships were destroyed", StringComparison.OrdinalIgnoreCase))
        {
            return MatchHistoryEndKind.NormalBattle;
        }

        var leavingUserId = TryParseLeavingUserId(finishReason);
        if (leavingUserId is null)
            return MatchHistoryEndKind.NormalBattle;

        return leavingUserId == userId
            ? MatchHistoryEndKind.SelfLeft
            : MatchHistoryEndKind.OpponentLeft;
    }

    private static int? TryParseLeavingUserId(string finishReason)
    {
        var abandonedShip = Regex.Match(
            finishReason,
            @"PlayerID\s+(\d+)\s+abandoned ship",
            RegexOptions.IgnoreCase);
        if (abandonedShip.Success && int.TryParse(abandonedShip.Groups[1].Value, out var abandonId))
            return abandonId;

        var forfeited = Regex.Match(
            finishReason,
            @"PlayerID\s+(\d+)\s+forfeited",
            RegexOptions.IgnoreCase);
        if (forfeited.Success && int.TryParse(forfeited.Groups[1].Value, out var forfeitId))
            return forfeitId;

        return null;
    }
}