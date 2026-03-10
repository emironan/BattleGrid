using BattleGrid.Contracts.RequestDtos;
using BattleGrid.Contracts.ResponseDtos;

namespace BattleGrid.Application.Interfaces
{
    public interface IAuthServices
    {
        Task<GeneralResponseDto> RegisterAsync(RegisterRequestDto dto);
    }
}