using BattleGrid.Contracts.ResponseDtos;

namespace BattleGrid.Application.Interfaces
{
    public interface IShipTypeServices
    {
        Task<List<ShipTypeListResponseDto>> ShipTypeListAsync();
    }
}