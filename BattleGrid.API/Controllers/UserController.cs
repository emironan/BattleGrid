
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
    public class UserController : ControllerBase
    {
        private readonly IUserServices _userService;
        private readonly BattleGridDbContext _context;

        public UserController(IUserServices userService, BattleGridDbContext context)
        {
            _userService = userService;
            _context = context;
        }

        [HttpGet("User/")]
        public async Task<ActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();

                if (users == null)
                {
                    return NotFound("No user found!");
                }

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occured while retrieving the users: {ex.Message}");
            }
        }

        [HttpGet("User/{userId}")]
        public async Task<ActionResult> GetUserById(int userId)
        {
            try
            {               
                var user = await _userService.GetUserAsync(userId);

                if (user == null) {
                    return NotFound("User not found!");
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occured while retrieving the user: {ex.Message}");
            }
        }
    }
}