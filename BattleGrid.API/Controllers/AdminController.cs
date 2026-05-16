using BattleGrid.API.Extensions;
using BattleGrid.Application.Interfaces;
using BattleGrid.Contracts.RequestDtos;
using BattleGrid.Contracts.ResponseDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BattleGrid.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AdminController : ControllerBase
{
    private readonly IAdminServices _adminServices;
    private readonly IUserServices _userServices;

    public AdminController(IAdminServices adminServices, IUserServices userServices)
    {
        _adminServices = adminServices;
        _userServices = userServices;
    }

    /// <summary>
    /// Ends the current competitive season and starts the next. Creates a new season stat row for the
    /// calling administrator; all other players receive a row when they enter matchmaking.
    /// </summary>
    [HttpPost("season/advance")]
    public async Task<ActionResult<AdvanceSeasonResponseDto>> AdvanceSeason([FromServices] IPlayerStatSeasonService playerStatSeason)
    {
        if (!User.TryGetAuthenticatedUserId(out var adminUserId))
            return Unauthorized();

        if (!await CallerIsAdminAsync(adminUserId))
            return Forbid();

        var result = await playerStatSeason.AdvanceSeasonAsync(adminUserId);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>Bans a player by username or email. Admin identity is taken from the JWT only.</summary>
    [HttpPost("players/ban")]
    public async Task<ActionResult<GeneralResponseDto>> BanPlayer([FromBody] BanRequestDto dto)
    {
        if (!User.TryGetAuthenticatedUserId(out var adminUserId))
            return Unauthorized();

        if (!await CallerIsAdminAsync(adminUserId))
            return Forbid();

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        dto.AdminID = adminUserId;

        var result = await _adminServices.BanPlayerAsync(dto);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary> Reverts the active ban for a player (temporary or permanent). Admin identity is taken from the JWT only. </summary>
    [HttpPost("players/unban")]
    public async Task<ActionResult<GeneralResponseDto>> UnbanPlayer([FromBody] UnbanPlayerRequestDto dto)
    {
        if (!User.TryGetAuthenticatedUserId(out var adminUserId))
            return Unauthorized();

        if (!await CallerIsAdminAsync(adminUserId))
            return Forbid();

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        dto.AdminID = adminUserId;

        var result = await _adminServices.UnbanPlayerAsync(dto);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary> Replay data for any match with a decisive outcome (admin only). </summary>
    [HttpGet("matches/{matchId:int}/replay")]
    public async Task<ActionResult<MatchReplayResponseDto>> GetMatchReplay(
        int matchId,
        [FromServices] IReplayServices replay)
    {
        if (!User.TryGetAuthenticatedUserId(out var adminUserId))
            return Unauthorized();

        if (!await CallerIsAdminAsync(adminUserId))
            return Forbid();

        var dto = await replay.GetMatchReplayForAdminAsync(matchId);
        if (dto is null)
            return NotFound();

        return Ok(dto);
    }

    private async Task<bool> CallerIsAdminAsync(int userId)
    {
        var user = await _userServices.GetByIdAsync(userId);
        return user is { IsAdmin: true };
    }
}