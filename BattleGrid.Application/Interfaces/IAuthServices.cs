using BattleGrid.Contracts.RequestDtos;
using BattleGrid.Contracts.ResponseDtos;

namespace BattleGrid.Application.Interfaces
{
    public interface IAuthServices
    {
        Task<GeneralResponseDto> RegisterAsync(RegisterRequestDto dto);
        Task<LoginResponseDto> LoginAsync(LoginRequestDto dto);
        Task<GeneralResponseDto> LogoutAsync(string refreshToken);
        Task<LoginResponseDto> RefreshAccessTokenAsync(string refreshToken);
        Task<GeneralResponseDto> VerifyPasswordAsync(LoginRequestDto dto);
        Task<GeneralResponseDto> UpdatePasswordAsync(PasswordUpdateRequestDto dto);
    }
}