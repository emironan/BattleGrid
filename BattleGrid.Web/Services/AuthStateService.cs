using BattleGrid.Contracts.ResponseDtos;
using BattleGrid.Web.Models;
using Microsoft.JSInterop;

namespace BattleGrid.Web.Services;

public class AuthStateService
{
    private readonly IJSRuntime _js;

    public AuthStateService(IJSRuntime js)
    {
        _js = js;
    }

    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTimeOffset? AccessTokenExpiry { get; private set; }
    public DateTimeOffset? RefreshTokenExpiry { get; private set; }
    public UserResponseDto? CurrentUser { get; private set; }
    public bool IsInitialized { get; private set; }
    public bool IsLoggedIn => AccessToken != null && CurrentUser != null;

    public event Action? OnChange;

    public async Task InitializeAsync()
    {
        if (IsInitialized) return;

        try
        {
            var state = await _js.InvokeAsync<AuthCookieSession?>("bgAuth.read");
            if (state is not null &&
                !string.IsNullOrWhiteSpace(state.AccessToken) &&
                state.CurrentUser is not null)
            {
                AccessToken = state.AccessToken;
                RefreshToken = state.RefreshToken;
                AccessTokenExpiry = state.AccessTokenExpiry;
                RefreshTokenExpiry = state.RefreshTokenExpiry;
                CurrentUser = state.CurrentUser;
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
        NotifyStateChanged();
    }

    public async Task LogoutAsync()
    {
        AccessToken = null;
        RefreshToken = null;
        AccessTokenExpiry = null;
        RefreshTokenExpiry = null;
        CurrentUser = null;
        await _js.InvokeVoidAsync("bgAuth.clear");
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}