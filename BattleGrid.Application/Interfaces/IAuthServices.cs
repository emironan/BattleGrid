using BattleGrid.Contracts.RequestDtos;
using BattleGrid.Contracts.ResponseDtos;

namespace BattleGrid.Application.Interfaces
{
    public interface IAuthServices
    {
        Task<GeneralResponseDto> RegisterAsync(RegisterRequestDto dto);
        Task<GeneralResponseDto> VerifyPasswordAsync(LoginRequestDto dto);
        Task<TokenResponseDto> LoginAsync(LoginRequestDto dto);
        Task<GeneralResponseDto> UpdatePasswordAsync(PasswordUpdateRequestDto dto);
    }
}