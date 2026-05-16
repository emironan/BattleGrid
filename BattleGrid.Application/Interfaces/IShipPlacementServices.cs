using BattleGrid.Contracts.RequestDtos;
using BattleGrid.Contracts.ResponseDtos;
using BattleGrid.Domain.Entities;
using BattleGrid.Domain.Enums;

namespace BattleGrid.Application.Interfaces
{
    public interface IShipPlacementServices
    {
        Task<GeneralResponseDto> PlaceShipAsync(PlaceShipRequestDto dto);

        /// <summary> Inserts rows in list order within a single transaction (preserves placement order). </summary>
        Task<GeneralResponseDto> SavePlacementsBatchAsync(int matchId, IReadOnlyList<(int PlayerID, int ShipID, int StartX, int StartY, bool IsVertical)> rowsInOrder);
    }
}