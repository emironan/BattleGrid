using BattleGrid.Contracts.RequestDtos;
using BattleGrid.Contracts.ResponseDtos;
using BattleGrid.Web.Models;
using Microsoft.JSInterop;
using System.Net.Http.Json;

namespace BattleGrid.Web.Services;

public class AuthStateService
{
    private readonly IJSRuntime _js;
    private readonly HttpClient _http;

    public AuthStateService(IJSRuntime js, HttpClient http)
    {
        _js = js;
        _http = http;
    }

    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTimeOffset? AccessTokenExpiry { get; private set; }
    public DateTimeOffset? RefreshTokenExpiry { get; private set; }
    public UserResponseDto? CurrentUser { get; private set; }
    public bool IsInitialized { get; private set; }
    public bool IsLoggedIn => AccessToken != null && CurrentUser != null;

    public event Action? OnChange;

    /// <summary>
    /// Loads session from cookies. The server-side /auth/session/read endpoint renews access tokens when needed.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (IsInitialized) return;

        try
        {
            var state = await _js.InvokeAsync<AuthCookieSession?>("bgAuth.read");
            if (state is not null && state.CurrentUser is not null)
            {
                AccessToken = state.AccessToken;
                RefreshToken = state.RefreshToken;
                AccessTokenExpiry = state.AccessTokenExpiry;
                RefreshTokenExpiry = state.RefreshTokenExpiry;
                CurrentUser = state.CurrentUser;
                await TryPersistProfileToLocalAsync();
            }
        }
        catch
        {
            // If storage is unavailable/corrupt, continue with anonymous state.
        }
        finally
        {
            IsInitialized = true;
            NotifyStateChanged();
        }
    }

    public async Task UpdateCurrentUserAsync(UserResponseDto user)
    {
        CurrentUser = user;
        if (AccessToken is null || RefreshToken is null)
        {
            NotifyStateChanged();
            return;
        }

        await _js.InvokeVoidAsync("bgAuth.store", new AuthCookieSession
        {
            AccessToken = AccessToken,
            RefreshToken = RefreshToken,
            AccessTokenExpiry = AccessTokenExpiry,
            RefreshTokenExpiry = RefreshTokenExpiry,
            CurrentUser = CurrentUser
        });
        await TryPersistProfileToLocalAsync();
        NotifyStateChanged();
    }

    public async Task SetAuthAsync(LoginResponseDto tokens, UserResponseDto user)
    {
        AccessToken = tokens.AccessToken;
        RefreshToken = tokens.RefreshToken;
        AccessTokenExpiry = tokens.ATExpiresAt;
        RefreshTokenExpiry = tokens.RTExpiresAt;
        CurrentUser = user;
        await _js.InvokeVoidAsync("bgAuth.store", new AuthCookieSession
        {
            AccessToken = AccessToken,
            RefreshToken = RefreshToken,
            AccessTokenExpiry = AccessTokenExpiry,
            RefreshTokenExpiry = RefreshTokenExpiry,
            CurrentUser = CurrentUser
        });
        await TryPersistProfileToLocalAsync();
        NotifyStateChanged();
    }

    public async Task LogoutAsync()
    {
        var refreshToken = RefreshToken;
        await TryLogoutAsync(refreshToken);

        AccessToken = null;
        RefreshToken = null;
        AccessTokenExpiry = null;
        RefreshTokenExpiry = null;
        CurrentUser = null;
        await _js.InvokeVoidAsync("bgAuth.clear");
        try
        {
            await _js.InvokeVoidAsync("bgAuth.clearBrowserStorages");
        }
        catch
        {
            // ignore missing script / prerender
        }

        NotifyStateChanged();
    }

    private async Task TryLogoutAsync(string? refreshToken)
    {
        try
        {
            await _http.PostAsJsonAsync("/api/Auth/logout", new RefreshTokenRequestDto
            {
                RefreshToken = refreshToken
            });
        }
        catch
        {
            // Local session is cleared even if the API call fails.
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

    private static string FormatDisplayForProfile(UserResponseDto user)
    {
        if (!string.IsNullOrWhiteSpace(user.UserName))
            return user.UserName;
        return $"user #{user.UserID}";
    }

    private async Task TryPersistProfileToLocalAsync()
    {
        if (CurrentUser is null)
            return;

        try
        {
            var label = FormatDisplayForProfile(CurrentUser);
            await _js.InvokeVoidAsync("battleGridProfile.setSelf", CurrentUser.UserID, label);
        }
        catch
        {
            // ignore missing script / prerender / storage quota
        }
    }
}
