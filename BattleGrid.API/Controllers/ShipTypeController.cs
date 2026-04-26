
using BattleGrid.Contracts.RequestDtos;
using BattleGrid.Application.Interfaces;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BattleGrid.Infrastructure.Data;
using BattleGrid.Domain.Entities;
using BattleGrid.Contracts.ResponseDtos;


namespace BattleGrid.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShipTypeController : ControllerBase
    {
        private readonly BattleGridDbContext _context;
        private readonly IShipTypeServices _shipTypeService;

        public ShipTypeController(BattleGridDbContext context, 
                                  IShipTypeServices shipTypeService)
        {
            _context = context;
            _shipTypeService = shipTypeService;
        }

        [HttpGet("all")]
        public async Task<ActionResult> ShipTypeList()
        {
            try
            {               
                var shipList = await _shipTypeService.ShipTypeListAsync();

                if (shipList == null) {
                    return NotFound("No ships found!");
                }

                return Ok(shipList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occured while retrieving the ship list: {ex.Message}");
            }
        }
    }
}