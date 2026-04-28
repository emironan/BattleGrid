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
        private readonly BattleGridDbContext _context;
        private readonly IUserServices _userServices;

        public UserController(BattleGridDbContext context,
                              IUserServices userService)
        {
            _context = context;
            _userServices = userService;
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

        [HttpPatch("bySystem")]
        public async Task<ActionResult> UserUpdateBySystem([FromBody] UserUpdateSystemRequestDto dto)
        {
            try
            {

                if (string.IsNullOrEmpty(dto.UserName) && !dto.BanLifted)
                {
                    return BadRequest("There is nothing to update!");
                }

                var user = await _userServices.GetByIdAsync(dto.UserID);

                if (user == null)
                {
                    return BadRequest("User does not exist!");
                }

                var result = await _userServices.UserUpdateBySystemAsync(dto);

                if (!result.Success)
                {
                    return BadRequest(result.Message);
                }

                return Ok(result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occured while updating the user: {ex.Message}");
            }
        }
    }
}