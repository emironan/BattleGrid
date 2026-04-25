using System;
using Microsoft.EntityFrameworkCore;
using BattleGrid.Domain.Entities;
using BattleGrid.Application.Interfaces;
using BattleGrid.Contracts.ResponseDtos;
using BattleGrid.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;
using BattleGrid.Contracts.RequestDtos;

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

        public async Task<bool> ValidatePlayer(int matchId, int playerId)
        {
            var PlayersOfAMatch = await _context.Match
                .Where(m => m.MatchID == matchId && (m.Player1ID == playerId || m.Player2ID == playerId))
                .FirstOrDefaultAsync();

            if(PlayersOfAMatch == null)
            {
                return false;
            }
            return true;
        }
    }
}