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
public class UserController : ControllerBase
{
    private readonly IUserServices _userServices;

    public UserController(IUserServices userService)
    {
        _userServices = userService;
    }

    [HttpGet("all")]
    public async Task<ActionResult> GetAllUsers()
    {
        if (!User.TryGetAuthenticatedUserId(out var callerId))
            return Unauthorized();

        if (!await _userServices.IsAdminAsync(callerId))
            return Forbid();

        try
        {
            var users = await _userServices.GetAllUsersAsync();

            if (users == null)
                return NotFound("No user found!");

            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occured while retrieving the users: {ex.Message}");
        }
    }

    [HttpGet("{userId:int}")]
    public async Task<ActionResult> GetUserById(int userId)
    {
        try
        {
            var user = await _userServices.GetByIdAsync(userId);

            if (user == null)
                return NotFound("User not found!");

            return Ok(user);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occured while retrieving the user: {ex.Message}");
        }
    }

    /// <summary> Competitive stats for the authenticated user for the active global season. </summary>
    [HttpGet("stats/current-season")]
    public async Task<ActionResult<PlayerSeasonStatsResponseDto>> GetMyCurrentSeasonStats(
        [FromServices] IPlayerStatSeasonService playerStatSeason)
    {
        if (!User.TryGetAuthenticatedUserId(out var uid))
            return Unauthorized();

        try
        {
            var stats = await playerStatSeason.GetCurrentSeasonStatsAsync(uid);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occured while retrieving season stats: {ex.Message}");
        }
    }

    /// <summary> Lookup by username or email (used before login). </summary>
    [HttpGet("{loginInfo}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByLoginInfo(string loginInfo)
    {
        try
        {
            var user = await _userServices.GetByLoginInfoAsync(loginInfo);

            if (user == null)
                return NotFound("User not found!");

            return Ok(user);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occured while retrieving the user info: {ex.Message}");
        }
    }
}