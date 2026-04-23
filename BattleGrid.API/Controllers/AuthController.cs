
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
        private readonly IAuthServices _authServices;
        private readonly BattleGridDbContext _context;

        public AuthController(IAuthServices authServices, BattleGridDbContext context)
        {
            _authServices = authServices;
            _context = context;
        }

        [HttpPost("/register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
        {
            try
            {
                var result = await _authServices.RegisterAsync(dto);
                if (!result.Success)
                {
                    //_logger.LogError("Registration failed - email already exists: {Email}", dto.Email);
                    return BadRequest($"An error occured while registering the user: {result.Message}");
                }

                return Ok(result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Registration error: {ex.Message}");
            }
        }

        [HttpPost("/login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            var tokenResponse = await _authServices.LoginAsync(dto);

            if (tokenResponse == null)
            {
                return Unauthorized("Incorrect username/email or password");
            }

            Response.Cookies.Append(
                "AccessToken",
                tokenResponse.AccessToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Expires = tokenResponse.ATExpiresAt,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/"
                });

            Response.Cookies.Append(
                "RefreshToken",
                tokenResponse.RefreshToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Expires = tokenResponse.RTExpiresAt,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/"
                });

            return Ok(new
            {
                message = "Login Successful",
                accessToken = tokenResponse.AccessToken,
                accessTokenExpiration = tokenResponse.ATExpiresAt,
                refreshToken = tokenResponse.RefreshToken,
                refreshTokenExpiration = tokenResponse.RTExpiresAt
            });

        }
    }
}