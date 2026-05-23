using BattleGrid.Application.Interfaces;
using BattleGrid.Contracts.RequestDtos;
using BattleGrid.Contracts.ResponseDtos;
using BattleGrid.Domain.Entities;
using BattleGrid.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BattleGrid.Application.Services
{
    public class UserServices : IUserServices
    {
        private readonly BattleGridDbContext _context;
        private readonly INormalizationHelper _normalizationHelper;

        public UserServices(BattleGridDbContext context,
                            INormalizationHelper normalizationHelper)
        {
            _context = context;
            _normalizationHelper = normalizationHelper;
        }

        public async Task<List<UserResponseDto>> GetAllUsersAsync()
        {
            var users = await _context.User
                .OrderBy(c => c.UserID)
                .Select(c => MapToResponseDto(c))
                .AsNoTracking()
                .ToListAsync();
            return users;
        }

        public async Task<UserResponseDto?> GetByIdAsync(int userId)
        {
            var user = await _context.User
                .Where(c => c.UserID == userId)
                .Select(c => MapToResponseDto(c))
                .AsNoTracking()
                .FirstOrDefaultAsync();
            return user;
        }

        // Find a user via UserName or Email
        public async Task<UserResponseDto?> GetByLoginInfoAsync(string loginInfo)
        {
            string normalizedLoginInfo = await _normalizationHelper.NormalizeLoginInfoAsync(loginInfo);

            var user = await _context.User
                .Where(c => c.UserName == normalizedLoginInfo || c.Email == normalizedLoginInfo)
                .Select(c => MapToResponseDto(c))
                .AsNoTracking()
                .FirstOrDefaultAsync();

            return user;
        }

        public async Task<string> HashPasswordAsync(int userId)
        {
            var user = await _context.User
                .Where(c => c.UserID == userId)
                .FirstOrDefaultAsync()
                ?? throw new Exception("User not found!"); // Redundant, AuthServices makes a null user check before calling

            //Redundant check since, we only call this service when the password is plain text in DB
            if (!user.PasswordHash.StartsWith("$2a$"))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
                user.LastUpdatedAt = DateTimeOffset.UtcNow.ToUniversalTime();
                user.UpdateReason = "Password hashed by the system";

                await _context.SaveChangesAsync();
            }

            // Get the same user again with updated password field
            var userNew = await _context.User
                .Where(c => c.UserID == userId)
                .AsNoTracking()
                .FirstOrDefaultAsync()
                ?? throw new Exception("User not found!"); // Redundant, after all the checks to see if the user is null

            // Check if the password is updated to a hashed version
            if (!userNew.PasswordHash.StartsWith("$2a$"))
            {
                throw new Exception("An error occured during password hash creation");
            }

            return userNew.PasswordHash;
        }

        //aaa TODO: UserUpdateByUserAsync
        //aaa TODO: UserUpdateByAdminAsync

        public async Task<bool> IsAdminAsync(int userId)
        {
            var user = await GetByIdAsync(userId);
            return user?.IsAdmin ?? false;
        }

        private static UserResponseDto MapToResponseDto(User user)
        {
            return new UserResponseDto
            {
                UserID = user.UserID,
                UserName = user.UserName,
                Email = user.Email,
                IsAdmin = user.IsAdmin,
                IsBanned = user.IsBanned,
                IsActive = user.IsActive
            };
        }
    }
}