
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
        private readonly IShipPlacementServices _shipPlacementService;
        private readonly BattleGridDbContext _context;

        public ShipPlacementController(IShipPlacementServices shipPlacementService, BattleGridDbContext context)
        {
            _shipPlacementService = shipPlacementService;
            _context = context;
        }

        [HttpPost("place-ship")]
        public async Task<IActionResult> PlaceShip([FromBody] PlaceShipRequestDto dto)
        {
            try
            {
                var result = await _shipPlacementService.PlaceShipAsync(dto);
                if (!result.Success)
                {
                    //_logger.LogError("Registration failed - email already exists: {Email}", dto.Email);
                    return BadRequest($"An error occured while registering the user: {result.Message}");
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