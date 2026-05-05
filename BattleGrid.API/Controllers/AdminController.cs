
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
    public class AdminController : ControllerBase
    {
        private readonly BattleGridDbContext _context;
        private readonly IAdminServices _adminServices;

        public AdminController(BattleGridDbContext context, 
                               IAdminServices adminServices)
        {
            _context = context;
            _adminServices = adminServices;
        }

        [HttpPost("banPlayer/{adminId}")]
        public async Task<IActionResult> BanPlayer([FromBody] BanRequestDto dto, int adminId)
        {
            try
            {
                dto.AdminID = adminId;

                var result = await _adminServices.BanPlayerAsync(dto);

                if (!result.Success)
                {
                    return BadRequest($"An error occured while registering the user: {result.Message}");
                }

                return Ok(result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Registration error: {ex.Message}");
            }
        }
    }
}