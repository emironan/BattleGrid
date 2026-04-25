using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BattleGrid.Contracts.RequestDtos;
using BattleGrid.Application.Interfaces;
using BattleGrid.Infrastructure.Data;
using BattleGrid.Domain.Entities;
using BattleGrid.Contracts.ResponseDtos;
using Microsoft.AspNetCore.Http.HttpResults;


namespace BattleGrid.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserServices _userServices;
        private readonly BattleGridDbContext _context;

        public UserController(IUserServices userService, BattleGridDbContext context)
        {
            _userServices = userService;
            _context = context;
        }

        [HttpGet("all")]
        public async Task<ActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userServices.GetAllUsersAsync();

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

        [HttpGet("{userId:int}")]
        public async Task<ActionResult> GetUserById(int userId)
        {
            try
            {               
                var user = await _userServices.GetByIdAsync(userId);

                if (user == null) 
                {
                    return NotFound("User not found!");
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occured while retrieving the user: {ex.Message}");
            }
        }

        [HttpGet("{loginInfo}")]
        public async Task<IActionResult> GetByLoginInfo(string loginInfo)
        {
            try
            {
                var user = await _userServices.GetByLoginInfoAsync(loginInfo);

                if (user == null)
                {
                    return NotFound("User not found!");
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occured while retrieving the user info: {ex.Message}");
            }
        }
    }
}