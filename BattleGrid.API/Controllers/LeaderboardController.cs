using BattleGrid.Application.Interfaces;
using BattleGrid.Contracts.ResponseDtos;
using Microsoft.AspNetCore.Mvc;

namespace BattleGrid.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class LeaderboardController : ControllerBase
{
    private readonly ILeaderboardService _leaderboardService;

    // Injecting the leaderboard business logic interface
    public LeaderboardController(ILeaderboardService leaderboardService)
    {
        _leaderboardService = leaderboardService;
    }

    /// <summary>
    /// Gets the ranking list for the active competitive season based on current ratings.
    /// </summary>
    [HttpGet("season")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<LeaderboardEntryDto>))]
    public async Task<IActionResult> GetCurrentSeasonLeaderboard(CancellationToken cancellationToken)
    {
        var result = await _leaderboardService.GetCurrentSeasonLeaderboardAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets the all-time ranking list based on the highest historical ratings achieved.
    /// </summary>
    [HttpGet("all-time")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<LeaderboardEntryDto>))]
    public async Task<IActionResult> GetAllTimeLeaderboard(CancellationToken cancellationToken)
    {
        var result = await _leaderboardService.GetAllTimeLeaderboardAsync(cancellationToken);
        return Ok(result);
    }
}