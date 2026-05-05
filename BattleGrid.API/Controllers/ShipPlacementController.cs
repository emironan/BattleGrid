
using BattleGrid.Contracts.RequestDtos;
using BattleGrid.Application.Interfaces;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BattleGrid.Infrastructure.Data;
using BattleGrid.Domain.Entities;


namespace BattleGrid.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShipPlacementController : ControllerBase
    {
        private readonly BattleGridDbContext _context;
        private readonly IShipPlacementServices _shipPlacementServices;
        private readonly IMatchServices _matchServices;

        public ShipPlacementController(BattleGridDbContext context,
                                       IShipPlacementServices shipPlacementServices,
                                       IMatchServices matchServices)
        {
            _context = context;
            _shipPlacementServices = shipPlacementServices;
            _matchServices = matchServices;
        }

        [HttpPost("placeShip")]
        public async Task<IActionResult> PlaceShip([FromBody] PlaceShipRequestDto dto)
        {
            try
            {
                // Check if the player sending the request is actually a player in that match
                var isPlayer = await _matchServices.ValidatePlayerAsync(dto.MatchID, dto.PlayerID);
                
                if(!isPlayer.Success)
                {
                    return Unauthorized($"You can not place this ship: {isPlayer.Message}");
                }

                var result = await _shipPlacementServices.PlaceShipAsync(dto);

                if (!result.Success)
                {
                    return BadRequest($"An error occured while placing the ship: {result.Message}");
                }

                return Ok(result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ship placement error: {ex.Message}");
            }
        }
    }
}