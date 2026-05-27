using BattleGrid.Application.Interfaces;
using BattleGrid.Contracts.ResponseDtos;
using BattleGrid.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BattleGrid.Application.Services;

public class LeaderboardService : ILeaderboardService
{
    private readonly BattleGridDbContext _context;

    public LeaderboardService(BattleGridDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<LeaderboardEntryDto>> GetCurrentSeasonLeaderboardAsync(CancellationToken cancellationToken = default)
    {
        bool hasStats = await _context.PlayerStat.AnyAsync(cancellationToken);

        int currentSeasonNo = hasStats
            ? await _context.PlayerStat.MaxAsync(ps => ps.SeasonNo, cancellationToken)
            : 1;

        var rawStats = await _context.PlayerStat
            .Where(ps => ps.SeasonNo == currentSeasonNo)
            .Join(_context.User, 
                ps => ps.UserID,
                u => u.UserID,
                (ps, u) => new LeaderboardEntryDto
                {
                    UserName = u.UserName,
                    Rating = ps.Rating,
                    MatchesPlayed = ps.MatchesPlayed,
                    MatchesWon = ps.MatchesWon,
                    WinRate = ps.WinRate,
                    SeasonNo = ps.SeasonNo
                })
            .OrderByDescending(x => x.Rating)
            .ToListAsync(cancellationToken);

        for (int i = 0; i < rawStats.Count; i++)
        {
            rawStats[i].Rank = i + 1;
        }

        return rawStats;
    }

    public async Task<IEnumerable<LeaderboardEntryDto>> GetAllTimeLeaderboardAsync(CancellationToken cancellationToken = default)
    {
        // 1. Veritabanındaki tüm satırları User tablosuyla ID'ler üzerinden düzgünce bağlıyoruz
        var rawStats = await _context.PlayerStat
            .Join(_context.User,
                ps => ps.UserID,
                u => u.UserID, // Hatalı olan u.User kısmını u.UserID olarak düzelttik!
                (ps, u) => new { ps, u })
            .ToListAsync(cancellationToken);

        // 2. Çektiğimiz verileri hafızada kullanıcı adına göre gruplayıp tekilleştiriyoruz
        var groupedStats = rawStats
            .GroupBy(x => x.u.UserName)
            .Select(g => {
                // Oyuncunun tüm kayıtları arasından HighestRating'i en yüksek olan en iyi sezonunu buluyoruz
                var bestSeason = g.OrderByDescending(x => x.ps.HighestRating).First();

                return new LeaderboardEntryDto
                {
                    UserName = g.Key,
                    // Tüm zamanlar tablosunda zirve noktayı (HighestRating) gösteriyoruz
                    Rating = bestSeason.ps.HighestRating,
                    // Kariyeri boyunca oynadığı toplam maçları topluyoruz
                    MatchesPlayed = g.Sum(x => x.ps.MatchesPlayed),
                    MatchesWon = g.Sum(x => x.ps.MatchesWon),
                    // Toplam kariyer galibiyet oranını hesaplıyoruz
                    WinRate = g.Sum(x => x.ps.MatchesPlayed) > 0
                        ? Math.Round((decimal)g.Sum(x => x.ps.MatchesWon) / g.Sum(x => x.ps.MatchesPlayed) * 100, 2)
                        : 0,
                    SeasonNo = bestSeason.ps.SeasonNo
                };
            })
            .OrderByDescending(x => x.Rating) // En yüksek zirve reytingine göre sırala
            .ToList();

        // 3. Sıralama numaralarını (Rank) baştan yazıyoruz
        for (int i = 0; i < groupedStats.Count; i++)
        {
            groupedStats[i].Rank = i + 1;
        }

        return groupedStats;
    }
}