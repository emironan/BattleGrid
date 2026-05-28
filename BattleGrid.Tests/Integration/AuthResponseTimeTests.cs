using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using BattleGrid.Contracts.RequestDtos;
using BattleGrid.Contracts.ResponseDtos;
using BattleGrid.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace BattleGrid.Tests.Integration;

/// <summary>
/// Measures register/login response time the same way the Blazor UI does:
/// timer starts when the form submit issues POST /api/Auth/register or /login,
/// and stops when the API returns success (after DB work completes server-side).
/// </summary>
public sealed class AuthResponseTimeTests : IClassFixture<BattleGridApiFactory>, IAsyncLifetime
{
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

        /* UserName prefix "perf" is used to identify test users in the database.
         * Normally, test users are deleted after each test. 
         * This will help identify the root cause if a test fails to delete
         * Default is "api". So, other tests do not have to specify a prefix
         */
        var request = ApiIntegrationTestHelper.CreateUniqueRegisterRequest("perf");

        // Same moment as Register.razor: loading = true; await Api.RegisterAsync(model);
        var stopwatch = Stopwatch.StartNew();
        using var response = await ApiIntegrationTestHelper.RegisterAsync(_client, request);
        stopwatch.Stop();

        var body = await response.Content.ReadAsStringAsync();
        var message = ApiIntegrationTestHelper.ParseRegisterResponseBody(body);

        _output.WriteLine($"Register response time: {stopwatch.ElapsedMilliseconds} ms");
        _output.WriteLine($"HTTP {(int)response.StatusCode} - {message}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ApiIntegrationTestHelper.ExpectedRegisterSuccessMessage, message);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BattleGridDbContext>();
        var persisted = await db.User.AsNoTracking()
            .AnyAsync(u => u.Email == request.Email && u.UserName == request.UserName);

        Assert.True(persisted, "User should exist in the database after a successful register response.");

        await ApiIntegrationTestHelper.CleanupTestUserAsync(db, request.Email);
    }

    [Fact]
    public async Task Register_MismatchedPasswords_ResponseTime_Measures_BadRequest()
    {
        if (!_databaseAvailable)
            return;

        var request = ApiIntegrationTestHelper.CreateUniqueRegisterRequest("perf");
        request.ConfirmPassword = "Mismatch_Password_456!";

        var stopwatch = Stopwatch.StartNew();
        using var response = await ApiIntegrationTestHelper.RegisterAsync(_client, request);
        stopwatch.Stop();

        _output.WriteLine($"Register mismatch-password response time: {stopwatch.ElapsedMilliseconds} ms");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await response.Content.ReadAsStringAsync();
    }

    [Fact]
    public async Task Login_ResponseTime_Measures_FromSubmit_UntilSuccessResponse()
    {
        if (!_databaseAvailable)
            return;

        var registerRequest = ApiIntegrationTestHelper.CreateUniqueRegisterRequest("perf");

        await ApiIntegrationTestHelper.RegisterUserForSetupAsync(_client, registerRequest);

        // Same moment as Login.razor: loading = true; await Api.LoginAsync(model);
        var stopwatch = Stopwatch.StartNew();
        using var response = await ApiIntegrationTestHelper.LoginPostAsync(
            _client, registerRequest.Email, registerRequest.Password);
        stopwatch.Stop();

        var loginResult = await response.Content.ReadFromJsonAsync<LoginResponseDto>();

        _output.WriteLine($"Login response time: {stopwatch.ElapsedMilliseconds} ms");
        _output.WriteLine($"HTTP {(int)response.StatusCode} - {loginResult?.Message}");

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

        await ApiIntegrationTestHelper.CleanupTestUserAsync(db, registerRequest.Email);
    }

    [Fact]
    public async Task Register_ResponseTime_Reports_Average_Over_Multiple_Runs()
    {
        if (!_databaseAvailable)
            return;

        const int iterations = 5;
        var timings = new List<long>(iterations);

        for (var i = 0; i < iterations; i++)
        {
            var request = ApiIntegrationTestHelper.CreateUniqueRegisterRequest("perf");

            var stopwatch = Stopwatch.StartNew();
            using var response = await ApiIntegrationTestHelper.RegisterAsync(_client, request);
            stopwatch.Stop();

            await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            timings.Add(stopwatch.ElapsedMilliseconds);

            await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, request.Email);
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

        for (var i = 0; i < iterations; i++)
        {
            var registerRequest = ApiIntegrationTestHelper.CreateUniqueRegisterRequest("perf");

            await ApiIntegrationTestHelper.RegisterUserForSetupAsync(_client, registerRequest);

            var stopwatch = Stopwatch.StartNew();
            using var response = await ApiIntegrationTestHelper.LoginPostAsync(
                _client, registerRequest.Email, registerRequest.Password);
            stopwatch.Stop();

            var loginResult = await response.Content.ReadFromJsonAsync<LoginResponseDto>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(loginResult);
            Assert.True(loginResult.Success);
            Assert.False(string.IsNullOrWhiteSpace(loginResult.AccessToken));
            Assert.False(string.IsNullOrWhiteSpace(loginResult.RefreshToken));

            timings.Add(stopwatch.ElapsedMilliseconds);

            await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, registerRequest.Email);
        }

        var average = timings.Average();
        var min = timings.Min();
        var max = timings.Max();

        _output.WriteLine($"Login timings (ms) over {iterations} runs: {string.Join(", ", timings)}");
        _output.WriteLine($"Login average: {average:F1} ms | min: {min} ms | max: {max} ms");

        Assert.True(average > 0);
    }

    [Fact]
    public async Task Logout_ResponseTime_Measures_FromSubmit_UntilSuccessResponse()
    {
        if (!_databaseAvailable)
            return;

        var request = ApiIntegrationTestHelper.CreateUniqueRegisterRequest("perf");
        await ApiIntegrationTestHelper.RegisterUserForSetupAsync(_client, request);
        var tokens = await ApiIntegrationTestHelper.LoginAsync(_client, request.Email, request.Password);
        Assert.NotNull(tokens);

        var stopwatch = Stopwatch.StartNew();
        using var response = await _client.PostAsJsonAsync(ApiIntegrationTestHelper.LogoutPath,
            new RefreshTokenRequestDto { RefreshToken = tokens.RefreshToken });
        stopwatch.Stop();

        var message = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Logout response time: {stopwatch.ElapsedMilliseconds} ms");
        _output.WriteLine($"HTTP {(int)response.StatusCode} - {message.Trim('\"')}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, request.Email);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ResponseTime_Measures_UntilBadRequest()
    {
        if (!_databaseAvailable)
            return;

        var request = ApiIntegrationTestHelper.CreateUniqueRegisterRequest("perf");
        await ApiIntegrationTestHelper.RegisterUserForSetupAsync(_client, request);

        var stopwatch = Stopwatch.StartNew();
        using var response = await ApiIntegrationTestHelper.RegisterAsync(_client, request);
        stopwatch.Stop();

        _output.WriteLine($"Register duplicate-email response time: {stopwatch.ElapsedMilliseconds} ms");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, request.Email);
    }

    [Fact]
    public async Task Login_WrongPassword_ResponseTime_Measures_UntilUnauthorized()
    {
        if (!_databaseAvailable)
            return;

        var request = ApiIntegrationTestHelper.CreateUniqueRegisterRequest("perf");
        await ApiIntegrationTestHelper.RegisterUserForSetupAsync(_client, request);

        var stopwatch = Stopwatch.StartNew();
        using var response = await ApiIntegrationTestHelper.LoginPostAsync(
            _client, request.Email, "wrong-password");
        stopwatch.Stop();

        _output.WriteLine($"Login wrong-password response time: {stopwatch.ElapsedMilliseconds} ms");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, request.Email);
    }

    [Fact]
    public async Task Login_NonExistingLoginInfo_ResponseTime_Measures_UntilUnauthorized()
    {
        if (!_databaseAvailable)
            return;

        var stopwatch = Stopwatch.StartNew();
        using var response = await ApiIntegrationTestHelper.LoginPostAsync(
            _client, $"missing_{Guid.NewGuid():N}@battlegrid.test", "wrong-password");
        stopwatch.Stop();

        _output.WriteLine($"Login missing-user response time: {stopwatch.ElapsedMilliseconds} ms");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ResponseTime_Measures_SuccessfulRequest()
    {
        if (!_databaseAvailable)
            return;

        var request = ApiIntegrationTestHelper.CreateUniqueRegisterRequest("perf");
        const string newPassword = "Perf_NewPassword_456!";
        await ApiIntegrationTestHelper.RegisterUserForSetupAsync(_client, request);
        var tokens = await ApiIntegrationTestHelper.LoginAsync(_client, request.Email, request.Password);
        Assert.NotNull(tokens);

        using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(_factory, tokens.AccessToken);

        var stopwatch = Stopwatch.StartNew();
        using var response = await authClient.PatchAsJsonAsync("/api/Auth/password", new PasswordUpdateRequestDto
        {
            OldPassword = request.Password,
            NewPassword = newPassword,
            ConfirmNewPassword = newPassword
        });
        stopwatch.Stop();

        _output.WriteLine($"Change password success response time: {stopwatch.ElapsedMilliseconds} ms");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, request.Email);
    }

    [Fact]
    public async Task ChangePassword_ResponseTime_Measures_FailedRequest()
    {
        if (!_databaseAvailable)
            return;

        var request = ApiIntegrationTestHelper.CreateUniqueRegisterRequest("perf");
        await ApiIntegrationTestHelper.RegisterUserForSetupAsync(_client, request);
        var tokens = await ApiIntegrationTestHelper.LoginAsync(_client, request.Email, request.Password);
        Assert.NotNull(tokens);

        using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(_factory, tokens.AccessToken);

        var stopwatch = Stopwatch.StartNew();
        using var response = await authClient.PatchAsJsonAsync("/api/Auth/password", new PasswordUpdateRequestDto
        {
            OldPassword = "wrong-old-password",
            NewPassword = "Any_NewPassword_456!",
            ConfirmNewPassword = "Any_NewPassword_456!"
        });
        stopwatch.Stop();

        _output.WriteLine($"Change password failed response time: {stopwatch.ElapsedMilliseconds} ms");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, request.Email);
    }

    [Fact]
    public async Task ChangePassword_MismatchedConfirmation_ResponseTime_Measures_FailedRequest()
    {
        if (!_databaseAvailable)
            return;

        var request = ApiIntegrationTestHelper.CreateUniqueRegisterRequest("perf");
        await ApiIntegrationTestHelper.RegisterUserForSetupAsync(_client, request);
        var tokens = await ApiIntegrationTestHelper.LoginAsync(_client, request.Email, request.Password);
        Assert.NotNull(tokens);

        using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(_factory, tokens.AccessToken);

        var stopwatch = Stopwatch.StartNew();
        using var response = await authClient.PatchAsJsonAsync("/api/Auth/password", new PasswordUpdateRequestDto
        {
            OldPassword = request.Password,
            NewPassword = "Mismatch_New_123!",
            ConfirmNewPassword = "Mismatch_New_456!"
        });
        stopwatch.Stop();

        _output.WriteLine($"Change password mismatch-confirmation response time: {stopwatch.ElapsedMilliseconds} ms");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, request.Email);
    }

    [Fact]
    public async Task ChangePassword_SameOldAndNew_ResponseTime_Measures_FailedRequest()
    {
        if (!_databaseAvailable)
            return;

        var request = ApiIntegrationTestHelper.CreateUniqueRegisterRequest("perf");
        await ApiIntegrationTestHelper.RegisterUserForSetupAsync(_client, request);
        var tokens = await ApiIntegrationTestHelper.LoginAsync(_client, request.Email, request.Password);
        Assert.NotNull(tokens);

        using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(_factory, tokens.AccessToken);

        var stopwatch = Stopwatch.StartNew();
        using var response = await authClient.PatchAsJsonAsync("/api/Auth/password", new PasswordUpdateRequestDto
        {
            OldPassword = request.Password,
            NewPassword = request.Password,
            ConfirmNewPassword = request.Password
        });
        stopwatch.Stop();

        _output.WriteLine($"Change password same-old-new response time: {stopwatch.ElapsedMilliseconds} ms");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, request.Email);
    }

    [Fact]
    public async Task ChangeEmail_ResponseTime_Measures_SuccessfulRequest()
    {
        if (!_databaseAvailable)
            return;

        var request = ApiIntegrationTestHelper.CreateUniqueRegisterRequest("perf");
        await ApiIntegrationTestHelper.RegisterUserForSetupAsync(_client, request);
        var tokens = await ApiIntegrationTestHelper.LoginAsync(_client, request.Email, request.Password);
        Assert.NotNull(tokens);

        using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(_factory, tokens.AccessToken);

        var stopwatch = Stopwatch.StartNew();
        using var response = await authClient.PatchAsJsonAsync("/api/User/email", new ChangeEmailRequestDto
        {
            NewEmail = $"updated_{Guid.NewGuid():N}@battlegrid.test",
            Password = request.Password
        });
        stopwatch.Stop();

        _output.WriteLine($"Change email success response time: {stopwatch.ElapsedMilliseconds} ms");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, request.Email);
    }

    [Fact]
    public async Task ChangeEmail_ResponseTime_Measures_FailedRequest()
    {
        if (!_databaseAvailable)
            return;

        var request = ApiIntegrationTestHelper.CreateUniqueRegisterRequest("perf");
        await ApiIntegrationTestHelper.RegisterUserForSetupAsync(_client, request);
        var tokens = await ApiIntegrationTestHelper.LoginAsync(_client, request.Email, request.Password);
        Assert.NotNull(tokens);

        using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(_factory, tokens.AccessToken);

        var stopwatch = Stopwatch.StartNew();
        using var response = await authClient.PatchAsJsonAsync("/api/User/email", new ChangeEmailRequestDto
        {
            NewEmail = $"updated_{Guid.NewGuid():N}@battlegrid.test",
            Password = "wrong-password"
        });
        stopwatch.Stop();

        _output.WriteLine($"Change email failed response time: {stopwatch.ElapsedMilliseconds} ms");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, request.Email);
    }

    [Fact]
    public async Task ChangeUsername_ResponseTime_Measures_SuccessfulRequest()
    {
        if (!_databaseAvailable)
            return;

        var request = ApiIntegrationTestHelper.CreateUniqueRegisterRequest("perf");
        await ApiIntegrationTestHelper.RegisterUserForSetupAsync(_client, request);
        var tokens = await ApiIntegrationTestHelper.LoginAsync(_client, request.Email, request.Password);
        Assert.NotNull(tokens);

        using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(_factory, tokens.AccessToken);

        var stopwatch = Stopwatch.StartNew();
        using var response = await authClient.PatchAsJsonAsync("/api/User/username", new ChangeUsernameRequestDto
        {
            NewUserName = $"renamed_{Guid.NewGuid():N}"[..18],
            Password = request.Password
        });
        stopwatch.Stop();

        _output.WriteLine($"Change username success response time: {stopwatch.ElapsedMilliseconds} ms");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, request.Email);
    }

    [Fact]
    public async Task ChangeUsername_ResponseTime_Measures_FailedRequest()
    {
        if (!_databaseAvailable)
            return;

        var request = ApiIntegrationTestHelper.CreateUniqueRegisterRequest("perf");
        await ApiIntegrationTestHelper.RegisterUserForSetupAsync(_client, request);
        var tokens = await ApiIntegrationTestHelper.LoginAsync(_client, request.Email, request.Password);
        Assert.NotNull(tokens);

        using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(_factory, tokens.AccessToken);

        var stopwatch = Stopwatch.StartNew();
        using var response = await authClient.PatchAsJsonAsync("/api/User/username", new ChangeUsernameRequestDto
        {
            NewUserName = $"renamed_{Guid.NewGuid():N}"[..18],
            Password = "wrong-password"
        });
        stopwatch.Stop();

        _output.WriteLine($"Change username failed response time: {stopwatch.ElapsedMilliseconds} ms");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, request.Email);
    }
}