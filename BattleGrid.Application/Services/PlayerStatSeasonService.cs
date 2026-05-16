using BattleGrid.Application.Interfaces;
using BattleGrid.Contracts.ResponseDtos;
using BattleGrid.Domain;
using BattleGrid.Domain.Entities;
using BattleGrid.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BattleGrid.Application.Services;

public sealed class PlayerStatSeasonService : IPlayerStatSeasonService

{
    private const decimal RatingDecayPerSeasonBehind = 0.20m;
    private const decimal RatingCarryoverPeakWeight = 0.65m;
    private const string PlayerStatUserSeasonUniqueConstraint = "PlayerStat_UserID_SeasonNo_key";

    private readonly BattleGridDbContext _context;

    public PlayerStatSeasonService(BattleGridDbContext context)
    {
        _context = context;
    }

    public async Task<PlayerStatSeasonState> EnsureMatchmakingRatingAsync( int userId, CancellationToken cancellationToken = default)
    {
        var currentSeason = await GetGlobalCurrentSeasonNoAsync(cancellationToken);
        var existing = await GetSeasonRowAsync(userId, currentSeason, cancellationToken);

        if (existing is not null)
            return await BuildStateFromRowAsync(existing, currentSeason, cancellationToken);

        var latest = await _context.PlayerStat.AsNoTracking()
            .Where(p => p.UserID == userId)
            .OrderByDescending(p => p.SeasonNo)
            .ThenByDescending(p => p.StatID)
            .FirstOrDefaultAsync(cancellationToken);

        if (latest is not null && latest.SeasonNo > currentSeason)
            return await BuildStateFromRowAsync(latest, latest.SeasonNo, cancellationToken);

        var rating = latest is null
            ? PlayerRatingBounds.ClampRating(PlayerRatingBounds.DefaultStartingRating)
            : CalculateCarryoverRating(latest, currentSeason);

        _context.PlayerStat.Add(CreateSeasonRow(userId, currentSeason, rating, rating));

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsPlayerStatSeasonUniqueViolation(ex))
        {
            DetachFailedPlayerStatEntries(ex);
            existing = await GetSeasonRowAsync(userId, currentSeason, cancellationToken)
                       ?? throw new InvalidOperationException(
                           "PlayerStat season row could not be created or loaded after a duplicate-key race.");

            return await BuildStateFromRowAsync(existing, currentSeason, cancellationToken);
        }
        return new PlayerStatSeasonState(rating, currentSeason);
    }

    public async Task<AdvanceSeasonResponseDto> AdvanceSeasonAsync(int adminUserId, CancellationToken cancellationToken = default)
    {
        var admin = await _context.User.AsNoTracking()
            .Where(u => u.UserID == adminUserId)
            .Select(u => new { u.UserID, u.IsAdmin })
            .FirstOrDefaultAsync(cancellationToken);

        if (admin is null || !admin.IsAdmin)
        {
            return new AdvanceSeasonResponseDto
            {
                Success = false,
                Message = "Only administrators can advance the competitive season."
            };
        }

        var previousSeason = await GetGlobalCurrentSeasonNoAsync(cancellationToken);
        var newSeason = previousSeason + 1;
        var existingNewSeason = await GetSeasonRowAsync(adminUserId, newSeason, cancellationToken);

        if (existingNewSeason is not null)
        {
            return new AdvanceSeasonResponseDto
            {
                Success = false,
                Message = $"Season {newSeason} is already active for this administrator.",
                PreviousSeasonNo = previousSeason,
                NewSeasonNo = newSeason
            };
        }

        var latest = await _context.PlayerStat.AsNoTracking()
            .Where(p => p.UserID == adminUserId)
            .OrderByDescending(p => p.SeasonNo)
            .ThenByDescending(p => p.StatID)
            .FirstOrDefaultAsync(cancellationToken);

        var startingRating = CalculateCarryoverRating(latest, newSeason);

        _context.PlayerStat.Add(CreateSeasonRow(adminUserId, newSeason, startingRating, startingRating));

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsPlayerStatSeasonUniqueViolation(ex))
        {
            DetachFailedPlayerStatEntries(ex);
            return new AdvanceSeasonResponseDto
            {
                Success = false,
                Message = $"Season {newSeason} is already active for this administrator.",
                PreviousSeasonNo = previousSeason,
                NewSeasonNo = newSeason
            };
        }

        return new AdvanceSeasonResponseDto
        {
            Success = true,
            Message = $"Season advanced from {previousSeason} to {newSeason}. Other players will receive a new season row when they enter matchmaking.",
            PreviousSeasonNo = previousSeason,
            NewSeasonNo = newSeason,
            AdminStartingRating = startingRating
        };
    }

    public async Task<PlayerSeasonStatsResponseDto> GetCurrentSeasonStatsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var state = await EnsureMatchmakingRatingAsync(userId, cancellationToken);
        var row = await _context.PlayerStat.AsNoTracking()
            .Where(p => p.UserID == userId && p.SeasonNo == state.GlobalCurrentSeason)
            .SingleAsync(cancellationToken);

        return new PlayerSeasonStatsResponseDto
        {
            SeasonNo = row.SeasonNo,
            MatchesPlayed = row.MatchesPlayed,
            MatchesWon = row.MatchesWon,
            WinRate = row.WinRate,
            Rating = row.Rating,
            HighestRating = row.HighestRating,
            LastUpdatedAt = row.LastUpdatedAt
        };
    }
    private async Task<PlayerStatSeasonState> BuildStateFromRowAsync(PlayerStat row, int seasonNo, CancellationToken cancellationToken)
    {
        await RepairInflatedSeasonPeakIfNeededAsync(row.UserID, seasonNo, row, cancellationToken);
        return new PlayerStatSeasonState(PlayerRatingBounds.ClampRating(row.Rating), seasonNo);
    }

    private Task<PlayerStat?> GetSeasonRowAsync(int userId, int seasonNo, CancellationToken cancellationToken) =>
        _context.PlayerStat.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserID == userId && p.SeasonNo == seasonNo, cancellationToken);

    private static int CalculateCarryoverRating(PlayerStat? latest, int targetSeason)
    {
        if (latest is null || latest.SeasonNo >= targetSeason)
            return PlayerRatingBounds.ClampRating(PlayerRatingBounds.DefaultStartingRating);

        var seasonsBehind = targetSeason - latest.SeasonNo;
        var ratingEnd = (decimal)PlayerRatingBounds.ClampRating(latest.Rating);
        var ratingPeak = (decimal)PlayerRatingBounds.ClampRating(latest.HighestRating);
        if (ratingPeak < ratingEnd)
            ratingPeak = ratingEnd;

        var w = RatingCarryoverPeakWeight;
        var carryBase = (1 - w) * ratingEnd + w * ratingPeak;
        var decayFactor = 1 - RatingDecayPerSeasonBehind * seasonsBehind;
        var raw = carryBase * decayFactor;
        return PlayerRatingBounds.ClampRating((int)Math.Round(raw));
    }

    private static PlayerStat CreateSeasonRow(int userId, int seasonNo, int rating, int highestRating) =>
        new()
        {
            UserID = userId,
            SeasonNo = seasonNo,
            MatchesPlayed = 0,
            MatchesWon = 0,
            WinRate = 0,
            Rating = rating,
            HighestRating = highestRating,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

    private async Task RepairInflatedSeasonPeakIfNeededAsync(int userId, int seasonNo, PlayerStat latest, CancellationToken cancellationToken)
    {
        if (latest.MatchesPlayed != 0 || latest.HighestRating <= latest.Rating)
            return;

        var row = await _context.PlayerStat
            .Where(p => p.UserID == userId && p.SeasonNo == seasonNo)
            .SingleAsync(cancellationToken);

        row.HighestRating = PlayerRatingBounds.ClampRating(row.Rating);
        row.LastUpdatedAt = DateTimeOffset.UtcNow.ToUniversalTime();
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<int> GetGlobalCurrentSeasonNoAsync(CancellationToken cancellationToken)
    {
        if (!await _context.PlayerStat.AsNoTracking().AnyAsync(cancellationToken))
            return 1;

        return await _context.PlayerStat.AsNoTracking().MaxAsync(p => p.SeasonNo, cancellationToken);
    }

    private static bool IsPlayerStatSeasonUniqueViolation(DbUpdateException ex)
    {
        for (var inner = ex.InnerException; inner is not null; inner = inner.InnerException)
        {
            if (inner is PostgresException pg
                && pg.SqlState == PostgresErrorCodes.UniqueViolation
                && (pg.ConstraintName == PlayerStatUserSeasonUniqueConstraint
                    || pg.ConstraintName?.Contains("UserID_SeasonNo", StringComparison.Ordinal) == true))
            {
                return true;
            }
        }

        return false;
    }

    private void DetachFailedPlayerStatEntries(DbUpdateException ex)
    {
        foreach (var entry in ex.Entries)
        {
            if (entry.Entity is PlayerStat)
                entry.State = EntityState.Detached;
        }
    }
}