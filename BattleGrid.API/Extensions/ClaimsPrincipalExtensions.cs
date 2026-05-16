using System.Security.Claims;

namespace BattleGrid.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetAuthenticatedUserId(this ClaimsPrincipal user, out int userId)
    {
        var idClaim = user.FindFirst("userId")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (idClaim is not null && int.TryParse(idClaim, out userId))
            return true;

        userId = 0;
        return false;
    }
}
