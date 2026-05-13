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
}