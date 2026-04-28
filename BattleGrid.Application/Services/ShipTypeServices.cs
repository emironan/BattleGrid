using BattleGrid.Application.Interfaces;
using BattleGrid.Contracts.ResponseDtos;
using BattleGrid.Domain.Entities;
using BattleGrid.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BattleGrid.Application.Services
{
    public class ShipTypeServices : IShipTypeServices
    {
        private readonly BattleGridDbContext _context;

        public ShipTypeServices(BattleGridDbContext context)
        {
            _context = context;
        }

        public async Task<List<ShipTypeListResponseDto>> ShipTypeListAsync()
        {
            var result = await _context.ShipType
                .Select(c => MapToResponseDto(c))
                .ToListAsync();
            return result;
        }

        private static ShipTypeListResponseDto MapToResponseDto(ShipType ship)
        {
            return new ShipTypeListResponseDto
            {
                ShipID = ship.ShipID,
                ShipName = ship.ShipName,
                Length = ship.Length,
                Width = ship.Width,
                MaxPerPlayer = ship.MaxPerPlayer,
            };
        }
    }
}
