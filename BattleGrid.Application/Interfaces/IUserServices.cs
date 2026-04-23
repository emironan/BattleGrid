using BattleGrid.Domain.Entities;
using BattleGrid.Contracts.ResponseDtos;

namespace BattleGrid.Application.Interfaces
{
    public interface IUserServices
    {
        Task<List<UserResponseDto>> GetAllUsersAsync();
        Task<UserResponseDto?> GetByIdAsync(int userId);
        Task<UserResponseDto?> GetByLoginInfoAsync(string loginInfo);   // UserName or Email
        Task<string> HashPasswordAsync(int userId);
    }
}