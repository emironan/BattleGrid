using BattleGrid.Application.Interfaces;
using BattleGrid.Contracts.ResponseDtos;
using BattleGrid.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BattleGrid.Application.Services
{
    public class MatchServices : IMatchServices
    {
        private readonly BattleGridDbContext _context;

        public MatchServices(BattleGridDbContext context)
        {
            _context = context;
        }

        public async Task<List<int>> GetPlayersAsync(int matchId)
        {
            var players = await _context.Match
                .Where(m => m.MatchID == matchId)
                .Select(m => m.Player1ID & m.Player2ID)
                .ToListAsync();

            return players;
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
    }
}