using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BattleGrid.Contracts.RequestDtos;
using BattleGrid.Contracts.ResponseDtos;
using BattleGrid.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BattleGrid.Tests.Integration;

internal static class ApiIntegrationTestHelper
{
    public const string RegisterPath = "/api/Auth/register";
    public const string LoginPath = "/api/Auth/login";
    public const string LogoutPath = "/api/Auth/logout";
    public const string RefreshPath = "/api/Auth/refresh";
    public const string ExpectedRegisterSuccessMessage = "User registered succesfully.";
    public const string DefaultTestPassword = "PerfTest_Pass123!";

    public static RegisterRequestDto CreateUniqueRegisterRequest(string userNamePrefix = "api")
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];
        return new RegisterRequestDto
        {
            UserName = $"{userNamePrefix}_{suffix}",
            Email = $"{userNamePrefix}_{suffix}@battlegrid.test",
            Password = DefaultTestPassword
        };
    }

    public static Task<HttpResponseMessage> LoginPostAsync(
        HttpClient client,
        string loginInfo,
        string password) =>
        client.PostAsJsonAsync(LoginPath, new LoginRequestDto
        {
            LoginInfo = loginInfo,
            Password = password
        });

    public static async Task<HttpResponseMessage> RegisterAsync(HttpClient client, RegisterRequestDto request) =>
        await client.PostAsJsonAsync(RegisterPath, request);

    public static async Task<(HttpStatusCode StatusCode, string Message)> RegisterAndReadAsync(
        HttpClient client,
        RegisterRequestDto request)
    {
        using var response = await RegisterAsync(client, request);
        var body = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, ParseRegisterResponseBody(body));
    }

    public static async Task<LoginResponseDto?> LoginAsync(HttpClient client, string loginInfo, string password)
    {
        using var response = await client.PostAsJsonAsync(LoginPath, new LoginRequestDto
        {
            LoginInfo = loginInfo,
            Password = password
        });

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<LoginResponseDto>();
    }

    public static void SetBearerToken(HttpClient client, string accessToken) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    public static void ClearBearerToken(HttpClient client) =>
        client.DefaultRequestHeaders.Authorization = null;

    public static HttpClient CreateAuthenticatedClient(BattleGridApiFactory factory, string accessToken)
    {
        var client = factory.CreateClient();
        SetBearerToken(client, accessToken);
        return client;
    }

    public static async Task<TestUserSession> CreateRegisteredUserAsync(HttpClient client)
    {
        var request = CreateUniqueRegisterRequest();
        var (status, _) = await RegisterAndReadAsync(client, request);
        if (status != HttpStatusCode.OK)
            throw new InvalidOperationException($"Failed to register test user: {status}");

        var profile = await client.GetFromJsonAsync<UserResponseDto>(
            $"/api/User/{Uri.EscapeDataString(request.Email)}");

        return new TestUserSession(request, profile);
    }

    public static async Task<TestUserSession> CreateLoggedInUserAsync(
        HttpClient client,
        BattleGridApiFactory factory,
        bool asAdmin = false)
    {
        var session = await CreateRegisteredUserAsync(client);
        if (asAdmin)
            await PromoteToAdminAsync(factory, session.Request.Email);

        var tokens = await LoginAsync(client, session.Request.Email, session.Request.Password);
        if (tokens is not { Success: true })
            throw new InvalidOperationException("Failed to log in test user.");

        var profile = await client.GetFromJsonAsync<UserResponseDto>(
            $"/api/User/{Uri.EscapeDataString(session.Request.Email)}");

        return session with { Tokens = tokens, Profile = profile };
    }

    public static async Task PromoteToAdminAsync(BattleGridApiFactory factory, string email)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BattleGridDbContext>();
        var user = await db.User.FirstAsync(u => u.Email == email);
        user.IsAdmin = true;
        await db.SaveChangesAsync();
    }

    public const string AdvanceSeasonPath = "/api/Admin/season/advance";

    public static Task<HttpResponseMessage> PostAdvanceSeasonAsync(HttpClient client) =>
        client.PostAsync(AdvanceSeasonPath, null);

    public static async Task DeletePlayerStatSeasonAsync(
        BattleGridApiFactory factory,
        int userId,
        int seasonNo)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BattleGridDbContext>();
        var row = await db.PlayerStat
            .FirstOrDefaultAsync(p => p.UserID == userId && p.SeasonNo == seasonNo);
        if (row is null)
            return;

        db.PlayerStat.Remove(row);
        await db.SaveChangesAsync();
    }

    public static string ParseRegisterResponseBody(string body) => body.Trim().Trim('"');

    public static async Task CleanupTestUserAsync(BattleGridApiFactory factory, string email)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BattleGridDbContext>();
        await CleanupTestUserAsync(db, email);
    }

    public static async Task CleanupTestUserAsync(BattleGridDbContext db, string email)
    {
        var user = await db.User.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null)
            return;

        var bans = await db.BanList
            .Where(b => b.PlayerID == user.UserID
                     || b.AdminID == user.UserID
                     || b.RevertingAdminID == user.UserID)
            .ToListAsync();
        if (bans.Count > 0)
            db.BanList.RemoveRange(bans);

        var sessions = await db.Session.Where(s => s.UserID == user.UserID).ToListAsync();
        if (sessions.Count > 0)
            db.Session.RemoveRange(sessions);

        var stats = await db.PlayerStat.Where(p => p.UserID == user.UserID).ToListAsync();
        if (stats.Count > 0)
            db.PlayerStat.RemoveRange(stats);

        db.User.Remove(user);
        await db.SaveChangesAsync();
    }
}

internal sealed record TestUserSession(
    RegisterRequestDto Request,
    UserResponseDto? Profile = null,
    LoginResponseDto? Tokens = null)
{
    public string Email => Request.Email;
    public string Password => Request.Password;
    public string UserName => Request.UserName;
}