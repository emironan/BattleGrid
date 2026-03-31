using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using BattleGrid.Application.Interfaces;
using BattleGrid.Contracts.ResponseDtos;
using BattleGrid.Infrastructure.Data;
using BattleGrid.Domain.Entities;
using BattleGrid.Domain.Enums;

namespace BattleGrid.Application.Services
{
    public class UserServices : IUserServices
    {
        private readonly BattleGridDbContext _context;

        public UserServices(BattleGridDbContext context)
        {
            _context = context;
        }

        public async Task<UserResponseDto?> GetUserAsync(int userId)
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
                IsAdmin = user.IsAdmin,
                IsBanned = user.IsBanned
            };
        }
    }
}