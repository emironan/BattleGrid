using System;
using Microsoft.EntityFrameworkCore;
using BattleGrid.Domain.Entities;
using BattleGrid.Domain.Enums;
using BattleGrid.Application.Interfaces;
using BattleGrid.Contracts.ResponseDtos;
using BattleGrid.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace BattleGrid.Application.Services
{
    public class UserServices : IUserServices
    {
        private readonly BattleGridDbContext _context;

        public UserServices(BattleGridDbContext context)
        {
            _context = context;
        }

        public async Task<UserResponseDto> GetUserAsync(int userId)
        {
            var user = await _context.User
                .Where(c => c.UserID == userId)
                .Select(c => MapToResponseDto(c))
                .FirstOrDefaultAsync();
            return user;
        }

        private static UserResponseDto MapToResponseDto(User user)
        {
            return new UserResponseDto
            {
                UserID = user.UserID,
                UserName = user.UserName,
                Email = user.Email,
                Role = user.Role,
                Rating = user.Rating,
                MatchesPlayed = user.MatchesPlayed,
                IsBanned = user.IsBanned
            };
        }
    }
}