using BattleGrid.Contracts.RequestDtos;
using BattleGrid.Contracts.ResponseDtos;

namespace BattleGrid.Application.Interfaces
{
    public interface IAdminServices
    {
        Task<GeneralResponseDto> BanPlayerAsync(BanRequestDto dto);
        //aaa Task<GeneralResponseDto> UndoPermanentBanAsync(UndoPermanentBanRequestDto dto);
        //aaa Task<GeneralResponseDto> UpdateBanStatusAsync(UpdateBanStatusRequestDto dto)
    }
}