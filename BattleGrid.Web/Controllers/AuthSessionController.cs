using BattleGrid.Contracts.ResponseDtos;
using BattleGrid.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BattleGrid.Web.Controllers;

[ApiController]
[Route("auth/session")]
public class AuthSessionController : ControllerBase
{
    [HttpGet("read")]
    public IActionResult Read()
    {
        if (!Request.Cookies.TryGetValue("bg_access_token", out var accessToken) ||
            !Request.Cookies.TryGetValue("bg_user", out var userPayload))
        {
            return NoContent();
        }

        AuthCookieUserPayload? payload = null;
        try
        {
            payload = JsonSerializer.Deserialize<AuthCookieUserPayload>(Uri.UnescapeDataString(userPayload));
        }
        catch
        {
            // malformed cookie payload, treat as anonymous
        }

        if (payload?.CurrentUser is null)
        {
            return NoContent();
        }

        Request.Cookies.TryGetValue("bg_refresh_token", out var refreshToken);

        return Ok(new AuthCookieSession
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiry = payload.AccessTokenExpiry,
            RefreshTokenExpiry = payload.RefreshTokenExpiry,
            CurrentUser = payload.CurrentUser
        });
    }

    [HttpPost("store")]
    public IActionResult Store([FromBody] AuthCookieSession state)
    {
        if (string.IsNullOrWhiteSpace(state.AccessToken) || state.CurrentUser is null)
        {
            return BadRequest("Missing access token or user.");
        }

        var cookieBase = new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        };

        var accessOptions = new CookieOptions
        {
            HttpOnly = cookieBase.HttpOnly,
            Secure = cookieBase.Secure,
            SameSite = cookieBase.SameSite,
            Path = cookieBase.Path,
            Expires = state.AccessTokenExpiry
        };

        var refreshOptions = new CookieOptions
        {
            HttpOnly = cookieBase.HttpOnly,
            Secure = cookieBase.Secure,
            SameSite = cookieBase.SameSite,
            Path = cookieBase.Path,
            Expires = state.RefreshTokenExpiry
        };

        Response.Cookies.Append("bg_access_token", state.AccessToken, accessOptions);
        if (!string.IsNullOrWhiteSpace(state.RefreshToken))
        {
            Response.Cookies.Append("bg_refresh_token", state.RefreshToken, refreshOptions);
        }

        var payload = new AuthCookieUserPayload
        {
            CurrentUser = state.CurrentUser,
            AccessTokenExpiry = state.AccessTokenExpiry,
            RefreshTokenExpiry = state.RefreshTokenExpiry
        };
        Response.Cookies.Append(
            "bg_user",
            Uri.EscapeDataString(JsonSerializer.Serialize(payload)),
            refreshOptions);

        return Ok();
    }

    [HttpPost("clear")]
    public IActionResult Clear()
    {
        Response.Cookies.Delete("bg_access_token", new CookieOptions { Path = "/" });
        Response.Cookies.Delete("bg_refresh_token", new CookieOptions { Path = "/" });
        Response.Cookies.Delete("bg_user", new CookieOptions { Path = "/" });
        return Ok();
    }
}

public class AuthCookieUserPayload
{
    public DateTimeOffset? AccessTokenExpiry { get; set; }
    public DateTimeOffset? RefreshTokenExpiry { get; set; }
    public UserResponseDto? CurrentUser { get; set; }
}
