using BattleGrid.Contracts.RequestDtos;
using BattleGrid.Contracts.ResponseDtos;
using BattleGrid.Domain.Entities;

namespace BattleGrid.Application.Interfaces
{
    public interface IUserServices
    {
        Task<List<UserResponseDto>> GetAllUsersAsync();
        Task<UserResponseDto?> GetByIdAsync(int userId);
        Task<UserResponseDto?> GetByLoginInfoAsync(string loginInfo);   // UserName or Email
        Task<string> HashPasswordAsync(int userId);
        Task<GeneralResponseDto> UserUpdateBySystemAsync(UserUpdateSystemRequestDto dto);
        //aaa Task<GeneralResponseDto> UserUpdateByUserAsync(UserUpdateUserRequestDto dto);
        //aaa Task<GeneralResponseDto> UserUpdateByAdminAsync(UserUpdateAdminRequestDto dto);
    }
}