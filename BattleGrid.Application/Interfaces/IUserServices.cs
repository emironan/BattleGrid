using BattleGrid.Contracts.RequestDtos;
using BattleGrid.Contracts.ResponseDtos;
using BattleGrid.Domain.Entities;

namespace BattleGrid.Application.Interfaces
{
    public interface IUserServices
    {
        Task<PagedUsersResponseDto> GetUsersPageAsync(int page, int pageSize);
        Task<UserResponseDto?> GetByIdAsync(int userId);
        Task<UserResponseDto?> GetByLoginInfoAsync(string loginInfo);   // UserName or Email
        Task<string> HashPasswordAsync(int userId);
        Task<bool> IsAdminAsync(int userId);

        /// <summary>Operator identity and season stats for admin personnel profile.</summary>
        Task<AdminUserProfileResponseDto?> GetAdminUserProfileAsync(int targetUserId, CancellationToken cancellationToken = default);
    }
}