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
    public class ShipPlacementServices : IShipPlacementServices
    {
        private readonly BattleGridDbContext _context;

        public ShipPlacementServices(BattleGridDbContext context)
        {
            _context = context;
        }

        public async Task<GeneralResponseDto> PlaceShipAsync(PlaceShipRequestDto dto)
        {
            var result = await _context.ShipPlacement.AddAsync(new ShipPlacement
            {
                PlayerID = dto.PlayerID,
                MatchID = dto.MatchID,
                ShipID = dto.ShipID,
                StartX = dto.StartX,
                StartY = dto.StartY,
                IsVertical = dto.IsVertical
            });
            await _context.SaveChangesAsync();

            if (result != null)
            {
                return new GeneralResponseDto
                {
                    Success = true,
                    Message = "Gemi başarıyla yerleştirildi."
                };
            }

            return new GeneralResponseDto
            {
                Success = false,
                Message = "Gemi yerleştirilirken bir hata oluştu."
            };
        }
    }
}