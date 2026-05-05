using BattleGrid.Contracts.RequestDtos;
using BattleGrid.Contracts.ResponseDtos;

namespace BattleGrid.Application.Interfaces
{
    public interface INormalizationHelper
    {
        Task<string> NormalizeLoginInfoAsync(string loginInfo);
    }
}