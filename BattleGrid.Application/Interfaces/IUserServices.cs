using BattleGrid.Contracts.ResponseDtos;

namespace BattleGrid.Application.Interfaces
{
    public interface IUserServices
    {
        Task<List<UserResponseDto>> GetAllUsersAsync();
        Task<UserResponseDto?> GetUserAsync(int userId);
    }
}