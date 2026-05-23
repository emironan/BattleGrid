using BattleGrid.Contracts.ResponseDtos;

namespace BattleGrid.Web.GameVisuals;

public static class UserAccountStatusHelper
{
    public static bool IsDeleted(UserResponseDto user) => !user.IsActive;

    public static string GetDataTableRowClass(UserResponseDto user)
    {
        if (IsDeleted(user))
            return "row-warning";
        if (user.IsBanned)
            return "row-danger";
        return string.Empty;
    }
}
