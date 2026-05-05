
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
    public class AuthController : ControllerBase
    {
        private readonly BattleGridDbContext _context;
        private readonly IAuthServices _authServices;

        public AuthController(BattleGridDbContext context, 
                              IAuthServices authServices)
        {
            _context = context;
            _authServices = authServices;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
        {
            try
            {
                var result = await _authServices.RegisterAsync(dto);
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

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            try
            {
                var loginResponse = await _authServices.LoginAsync(dto);

                // Just in case
                if (loginResponse == null)
                {
                    return Unauthorized("Incorrect username/email or password.");
                }

                // If login failed, return the error message
                if (!loginResponse.Success)
                {
                    return Unauthorized(loginResponse.Message);
                }

                // Continue with successful login
                // Set the cookies for access and refresh tokens
                Response.Cookies.Append(
                    "AccessToken",
                    loginResponse.AccessToken,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Expires = loginResponse.ATExpiresAt,
                        Secure = Request.IsHttps,
                        SameSite = SameSiteMode.Lax,
                        Path = "/"
                    });

                Response.Cookies.Append(
                    "RefreshToken",
                    loginResponse.RefreshToken,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Expires = loginResponse.RTExpiresAt,
                        Secure = Request.IsHttps,
                        SameSite = SameSiteMode.Lax,
                        Path = "/"
                    });

                return Ok(loginResponse);
            }
            catch
            {
                return StatusCode(500, "An error occured during login.");
            }

        }

        [HttpPatch("password")]
        public async Task<IActionResult> UpdatePassword([FromBody] PasswordUpdateRequestDto dto, int userId)
        {
            try
            {
                dto.UserID = userId;

                var result = await _authServices.UpdatePasswordAsync(dto);
                if (!result.Success)
                {
                    return BadRequest($"An error occured while updating password: {result.Message}");
                }

                return Ok(result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occured while updating password: {ex.Message}");
            }
        }
    }
}