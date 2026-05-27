using System.Diagnostics;
using Xunit.Abstractions;
using System.Net;
using System.Net.Http.Json;
using BattleGrid.Contracts.RequestDtos;
using BattleGrid.Contracts.ResponseDtos;
using BattleGrid.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BattleGrid.Tests.Integration;

/// <summary>
/// Measures register/login response time the same way the Blazor UI does:
/// timer starts when the form submit issues POST /api/Auth/register or /login,
/// and stops when the API returns success (after DB work completes server-side).
/// </summary>
public sealed class AuthResponseTimeTests : IClassFixture<BattleGridApiFactory>, IAsyncLifetime
{
    private const string RegisterPath = "/api/Auth/register";
    private const string LoginPath = "/api/Auth/login";
    private const string ExpectedRegisterSuccessMessage = "User registered succesfully.";

    private readonly BattleGridApiFactory _factory;
    private readonly ITestOutputHelper _output;
    private HttpClient _client = null!;
    private bool _databaseAvailable;

    public AuthResponseTimeTests(BattleGridApiFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        _databaseAvailable = await _factory.CanConnectToDatabaseAsync();
        if (!_databaseAvailable)
        {
            _output.WriteLine("PostgreSQL is not reachable. Skipping auth response-time tests.");
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Register_ResponseTime_Measures_FromSubmit_UntilSuccessResponse()
    {
        if (!_databaseAvailable)
            return;

        var suffix = Guid.NewGuid().ToString("N")[..12];
        var request = new RegisterRequestDto
        {
            UserName = $"perf_{suffix}",
            Email = $"perf_{suffix}@battlegrid.test",
            Password = "PerfTest_Pass123!"
        };

        // Same moment as Register.razor: loading = true; await Api.RegisterAsync(model);
        var stopwatch = Stopwatch.StartNew();
        using var response = await _client.PostAsJsonAsync(RegisterPath, request);
        stopwatch.Stop();

        var body = await response.Content.ReadAsStringAsync();
        var message = ParseRegisterResponseBody(body);

        _output.WriteLine($"Register response time: {stopwatch.ElapsedMilliseconds} ms");
        _output.WriteLine($"HTTP {(int)response.StatusCode} — {message}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ExpectedRegisterSuccessMessage, message);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BattleGridDbContext>();
        var persisted = await db.User.AsNoTracking()
            .AnyAsync(u => u.Email == request.Email && u.UserName == request.UserName);

        Assert.True(persisted, "User should exist in the database after a successful register response.");

        await CleanupTestUserAsync(db, request.Email);
    }

    [Fact]
    public async Task Login_ResponseTime_Measures_FromSubmit_UntilSuccessResponse()
    {
        if (!_databaseAvailable)
            return;

        var suffix = Guid.NewGuid().ToString("N")[..12];
        const string password = "PerfTest_Pass123!";
        var registerRequest = new RegisterRequestDto
        {
            UserName = $"perf_{suffix}",
            Email = $"perf_{suffix}@battlegrid.test",
            Password = password
        };

        using var registerResponse = await _client.PostAsJsonAsync(RegisterPath, registerRequest);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginRequest = new LoginRequestDto
        {
            LoginInfo = registerRequest.Email,
            Password = password
        };

        // Same moment as Login.razor: loading = true; await Api.LoginAsync(model);
        var stopwatch = Stopwatch.StartNew();
        using var response = await _client.PostAsJsonAsync(LoginPath, loginRequest);
        stopwatch.Stop();

        var loginResult = await response.Content.ReadFromJsonAsync<LoginResponseDto>();

        _output.WriteLine($"Login response time: {stopwatch.ElapsedMilliseconds} ms");
        _output.WriteLine($"HTTP {(int)response.StatusCode} — {loginResult?.Message}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(loginResult);
        Assert.True(loginResult.Success);
        Assert.False(string.IsNullOrWhiteSpace(loginResult.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(loginResult.RefreshToken));

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BattleGridDbContext>();
        var sessionExists = await db.Session.AsNoTracking()
            .AnyAsync(s => s.RefreshToken == loginResult.RefreshToken);

        Assert.True(sessionExists, "Login should persist a session before returning tokens.");

        await CleanupTestUserAsync(db, registerRequest.Email);
    }

    [Fact]
    public async Task Register_ResponseTime_Reports_Average_Over_Multiple_Runs()
    {
        if (!_databaseAvailable)
            return;

        const int iterations = 5;
        var timings = new List<long>(iterations);

        for (int i = 0; i < iterations; i++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..12];

            var request = new RegisterRequestDto
            {
                UserName = $"perf_{suffix}",
                Email = $"perf_{suffix}@battlegrid.test",
                Password = "PerfTest_Pass123!"
            };

            var stopwatch = Stopwatch.StartNew();
            using var response = await _client.PostAsJsonAsync(RegisterPath, request);
            stopwatch.Stop();
            
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            timings.Add(stopwatch.ElapsedMilliseconds);

            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<BattleGridDbContext>();
            await CleanupTestUserAsync(db, request.Email);
        }

        var average = timings.Average();
        var min = timings.Min();
        var max = timings.Max();

        _output.WriteLine($"Register timings (ms) over {iterations} runs: {string.Join(", ", timings)}");
        _output.WriteLine($"Register average: {average:F1} ms | min: {min} ms | max: {max} ms");

        Assert.True(average > 0);
    }

    [Fact]
    public async Task Login_ResponseTime_Reports_Average_Over_Multiple_Runs()
    {
        if (!_databaseAvailable)
            return;
        
        const int iterations = 5;
        var timings = new List<long>(iterations);

        for (int i = 0; i < iterations; i++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..12];
            const string password = "PerfTest_Pass123!";

            var registerRequest = new RegisterRequestDto
            {
                UserName = $"perf_{suffix}",
                Email = $"perf_{suffix}@battlegrid.test",
                Password = password
            };

            using var registerResponse = await _client.PostAsJsonAsync(RegisterPath, registerRequest);
            Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

            var loginRequest = new LoginRequestDto
            {
                LoginInfo = registerRequest.Email,
                Password = password
            };

            // Same moment as Login.razor: loading = true; await Api.LoginAsync(model);
            var stopwatch = Stopwatch.StartNew();
            using var response = await _client.PostAsJsonAsync(LoginPath, loginRequest);
            stopwatch.Stop();

            var loginResult = await response.Content.ReadFromJsonAsync<LoginResponseDto>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(loginResult);
            Assert.True(loginResult.Success);
            Assert.False(string.IsNullOrWhiteSpace(loginResult.AccessToken));
            Assert.False(string.IsNullOrWhiteSpace(loginResult.RefreshToken));

            timings.Add(stopwatch.ElapsedMilliseconds);

            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<BattleGridDbContext>();
            await CleanupTestUserAsync(db, registerRequest.Email);
        }

        var average = timings.Average();
        var min = timings.Min();
        var max = timings.Max();

        _output.WriteLine($"Login timings (ms) over {iterations} runs: {string.Join(", ", timings)}");
        _output.WriteLine($"Login average: {average:F1} ms | min: {min} ms | max: {max} ms");

        Assert.True(average > 0);
    }

    /// <summary> Matches <see cref="BattleGrid.Web.Services.ApiService.RegisterAsync"/> body handling. </summary>
    private static string ParseRegisterResponseBody(string body) => body.Trim().Trim('"');

    /// <summary> Deletes test users from database after tests are complete. </summary>
    private static async Task CleanupTestUserAsync(BattleGridDbContext db, string email)
    {
        var user = await db.User.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null)
            return;

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