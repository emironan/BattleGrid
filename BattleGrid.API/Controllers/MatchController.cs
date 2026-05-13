using System.Security.Claims;
using BattleGrid.Application.Interfaces;
using BattleGrid.Contracts.ResponseDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BattleGrid.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class MatchController : ControllerBase
{
    [HttpGet("resumable")]
    public async Task<ActionResult<ResumableMatchResponseDto>> Resumable([FromServices] IMatchServices matches)
    {
        var idClaim = User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (idClaim is null || !int.TryParse(idClaim, out var uid))
            return Unauthorized();

        var mid = await matches.GetResumableMatchIdAsync(uid);
        if (mid is null)
            return NoContent();

        return Ok(new ResumableMatchResponseDto { MatchId = mid.Value });
    }
}
