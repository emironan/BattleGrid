using BattleGrid.Contracts.ResponseDtos;

namespace BattleGrid.Application.Interfaces
{
    public interface IUserServices
    {
        Task<UserResponseDto> GetUserAsync(int userId);
    }
}
