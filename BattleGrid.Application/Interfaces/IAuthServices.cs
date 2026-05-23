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

        Task<(GeneralResponseDto Result, UserResponseDto? UpdatedUser)> ChangeEmailAsync(
            int userId,
            ChangeEmailRequestDto dto,
            CancellationToken cancellationToken = default);

        Task<(GeneralResponseDto Result, UserResponseDto? UpdatedUser)> ChangeUsernameAsync(
            int userId,
            ChangeUsernameRequestDto dto,
            CancellationToken cancellationToken = default);

        Task<GeneralResponseDto> DeactivateAccountAsync(
            int userId,
            DeactivateAccountRequestDto dto,
            CancellationToken cancellationToken = default);
    }
}