using BattleGrid.Contracts.RequestDtos;
using BattleGrid.Contracts.ResponseDtos;

namespace BattleGrid.Application.Interfaces
{
    public interface IShipPlacementServices
    {
        Task<GeneralResponseDto> PlaceShipAsync(PlaceShipRequestDto dto);
        //aaa Task<GeneralResponseDto> GetAllPlacementsInAMatch(dto);
        //aaa Task<GeneralResponseDto> GetAllPlacementsOfAPlayerInAMatch(dto);
    }
}