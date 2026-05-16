using BattleGrid.Contracts.RequestDtos;
using BattleGrid.Contracts.ResponseDtos;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BattleGrid.Web.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private readonly AuthStateService _auth;

    public ApiService(HttpClient http, AuthStateService auth)
    {
        _http = http;
        _auth = auth;
    }

    private void ApplyAuth()
    {
        if (_auth.AccessToken != null)
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
        else
            _http.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<(bool Success, string Message)> RegisterAsync(RegisterRequestDto request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/Auth/register", request);
            var body = await response.Content.ReadAsStringAsync();
            return (response.IsSuccessStatusCode, body.Trim('"'));
        }
        catch (Exception ex)
        {
            return (false, $"Connection error: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message, LoginResponseDto? Data)> LoginAsync(LoginRequestDto request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/Auth/login", request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return (false, error.Trim('"'), null);
            }
            var data = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
            return (true, data?.Message ?? "Login successful.", data);
        }
        catch (Exception ex)
        {
            return (false, $"Connection error: {ex.Message}", null);
        }
    }

    public async Task LogoutAsync(string? refreshToken)
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

    public async Task<(bool Success, LoginResponseDto? Data)> RefreshAccessTokenAsync(string refreshToken)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/Auth/refresh", new RefreshTokenRequestDto
            {
                RefreshToken = refreshToken
            });
            if (!response.IsSuccessStatusCode)
            {
                return (false, null);
            }

            var data = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
            return (data?.Success == true, data);
        }
        catch
        {
            return (false, null);
        }
    }

    public async Task<List<UserResponseDto>?> GetAllUsersAsync()
    {
        ApplyAuth();
        try
        {
            return await _http.GetFromJsonAsync<List<UserResponseDto>>("/api/User/all");
        }
        catch
        {
            return null;
        }
    }

    public async Task<UserResponseDto?> GetUserByLoginInfoAsync(string loginInfo)
    {
        try
        {
            return await _http.GetFromJsonAsync<UserResponseDto>(
                $"/api/User/{Uri.EscapeDataString(loginInfo)}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<PlayerSeasonStatsResponseDto?> GetMyCurrentSeasonStatsAsync()
    {
        ApplyAuth();
        try
        {
            return await _http.GetFromJsonAsync<PlayerSeasonStatsResponseDto>("/api/User/stats/current-season");
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> CheckApiHealthAsync()
    {
        try
        {
            var resp = await _http.GetAsync("/");
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<ShipTypeListResponseDto>?> GetShipTypesAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<ShipTypeListResponseDto>>("/api/ShipType/all");
        }
        catch
        {
            return null;
        }
    }

    public async Task<ResumableMatchResponseDto?> GetResumableMatchAsync()
    {
        ApplyAuth();
        try
        {
            var resp = await _http.GetAsync("/api/Match/resumable");
            if (resp.StatusCode == HttpStatusCode.NoContent)
                return null;
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<ResumableMatchResponseDto>();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Returns persisted terminal match state when the DB row is already finished (won/abandoned).
    /// <c>null</c> when the match is still active or the user is not a participant.
    /// </summary>
    public async Task<MatchRecoveryStateDto?> GetMatchRecoveryIfTerminalAsync(int matchId)
    {
        ApplyAuth();
        try
        {
            var resp = await _http.GetAsync($"/api/Match/{matchId}/recovery");
            if (resp.StatusCode == HttpStatusCode.NoContent)
                return null;
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<MatchRecoveryStateDto>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<MatchHistoryResponseDto?> GetMatchHistoryAsync(int limit = 20)
    {
        ApplyAuth();
        try
        {
            var capped = Math.Clamp(limit, 1, 20);
            return await _http.GetFromJsonAsync<MatchHistoryResponseDto>($"/api/Match/history?limit={capped}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<MatchReplayResponseDto?> GetMatchReplayAsync(int matchId)
    {
        ApplyAuth();
        try
        {
            return await _http.GetFromJsonAsync<MatchReplayResponseDto>($"/api/Match/{matchId}/replay");
        }
        catch
        {
            return null;
        }
    }

    public async Task<MatchReplayResponseDto?> GetAdminMatchReplayAsync(int matchId)
    {
        ApplyAuth();
        try
        {
            return await _http.GetFromJsonAsync<MatchReplayResponseDto>($"/api/Admin/matches/{matchId}/replay");
        }
        catch
        {
            return null;
        }
    }

    public async Task<AdvanceSeasonResponseDto?> AdvanceSeasonAsync()
    {
        ApplyAuth();
        try
        {
            var resp = await _http.PostAsync("/api/Admin/season/advance", null);
            return await resp.Content.ReadFromJsonAsync<AdvanceSeasonResponseDto>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<GeneralResponseDto?> BanPlayerAsync(BanRequestDto request)
    {
        ApplyAuth();
        try
        {
            var resp = await _http.PostAsJsonAsync("/api/Admin/players/ban", request);
            return await resp.Content.ReadFromJsonAsync<GeneralResponseDto>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<GeneralResponseDto?> UnbanPlayerAsync(UnbanPlayerRequestDto request)
    {
        ApplyAuth();
        try
        {
            var resp = await _http.PostAsJsonAsync("/api/Admin/players/unban", request);
            return await resp.Content.ReadFromJsonAsync<GeneralResponseDto>();
        }
        catch
        {
            return null;
        }
    }
}