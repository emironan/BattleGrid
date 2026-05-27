using System.Net;
using System.Net.Http.Json;
using BattleGrid.Contracts.RequestDtos;
using BattleGrid.Contracts.ResponseDtos;

namespace BattleGrid.Tests.Integration;

public sealed class AuthEndpointTests : IClassFixture<BattleGridApiFactory>, IAsyncLifetime
{
    private readonly BattleGridApiFactory _factory;
    private HttpClient _client = null!;
    private bool _databaseAvailable;

    public AuthEndpointTests(BattleGridApiFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        _databaseAvailable = await _factory.CanConnectToDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Register_WithValidPayload_ReturnsOk()
    {
        if (!_databaseAvailable)
            return;

        var request = ApiIntegrationTestHelper.CreateUniqueRegisterRequest();
        try
        {
            var (status, message) = await ApiIntegrationTestHelper.RegisterAndReadAsync(_client, request);
            Assert.Equal(HttpStatusCode.OK, status);
            Assert.Equal(ApiIntegrationTestHelper.ExpectedRegisterSuccessMessage, message);
        }
        finally
        {
            await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, request.Email);
        }
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        if (!_databaseAvailable)
            return;

        var request = ApiIntegrationTestHelper.CreateUniqueRegisterRequest();
        try
        {
            var first = await ApiIntegrationTestHelper.RegisterAsync(_client, request);
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            await first.Content.ReadAsStringAsync();

            using var duplicate = await ApiIntegrationTestHelper.RegisterAsync(_client, request);
            Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        }
        finally
        {
            await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, request.Email);
        }
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        if (!_databaseAvailable)
            return;

        var request = ApiIntegrationTestHelper.CreateUniqueRegisterRequest();
        try
        {
            await ApiIntegrationTestHelper.RegisterAsync(_client, request);

            var tokens = await ApiIntegrationTestHelper.LoginAsync(
                _client, request.Email, request.Password);

            Assert.NotNull(tokens);
            Assert.True(tokens.Success);
            Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
            Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
        }
        finally
        {
            await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, request.Email);
        }
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        if (!_databaseAvailable)
            return;

        var request = ApiIntegrationTestHelper.CreateUniqueRegisterRequest();
        try
        {
            await ApiIntegrationTestHelper.RegisterAsync(_client, request);

            using var response = await _client.PostAsJsonAsync(ApiIntegrationTestHelper.LoginPath, new LoginRequestDto
            {
                LoginInfo = request.Email,
                Password = "wrong-password"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, request.Email);
        }
    }

    [Fact]
    public async Task Logout_WithRefreshToken_ReturnsOk()
    {
        if (!_databaseAvailable)
            return;

        var request = ApiIntegrationTestHelper.CreateUniqueRegisterRequest();
        try
        {
            await ApiIntegrationTestHelper.RegisterAsync(_client, request);
            var tokens = await ApiIntegrationTestHelper.LoginAsync(
                _client, request.Email, request.Password);
            Assert.NotNull(tokens);

            using var response = await _client.PostAsJsonAsync(ApiIntegrationTestHelper.LogoutPath,
                new RefreshTokenRequestDto { RefreshToken = tokens.RefreshToken });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        finally
        {
            await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, request.Email);
        }
    }

    [Fact]
    public async Task Refresh_WithValidRefreshToken_ReturnsNewAccessToken()
    {
        if (!_databaseAvailable)
            return;

        var request = ApiIntegrationTestHelper.CreateUniqueRegisterRequest();
        try
        {
            await ApiIntegrationTestHelper.RegisterAsync(_client, request);
            var tokens = await ApiIntegrationTestHelper.LoginAsync(
                _client, request.Email, request.Password);
            Assert.NotNull(tokens);

            using var response = await _client.PostAsJsonAsync(ApiIntegrationTestHelper.RefreshPath,
                new RefreshTokenRequestDto { RefreshToken = tokens.RefreshToken });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var refreshed = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
            Assert.NotNull(refreshed);
            Assert.True(refreshed.Success);
            Assert.False(string.IsNullOrWhiteSpace(refreshed.AccessToken));
        }
        finally
        {
            await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, request.Email);
        }
    }

    [Fact]
    public async Task UpdatePassword_WithoutAuth_ReturnsUnauthorized()
    {
        if (!_databaseAvailable)
            return;

        using var response = await _client.PatchAsJsonAsync("/api/Auth/password", new PasswordUpdateRequestDto
        {
            OldPassword = "old",
            NewPassword = "new"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePassword_WithAuth_ReturnsOk()
    {
        if (!_databaseAvailable)
            return;

        var request = ApiIntegrationTestHelper.CreateUniqueRegisterRequest();
        const string newPassword = "NewSecure_Pass456!";
        try
        {
            await ApiIntegrationTestHelper.RegisterAsync(_client, request);
            var tokens = await ApiIntegrationTestHelper.LoginAsync(
                _client, request.Email, request.Password);
            Assert.NotNull(tokens);

            using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(
                _factory, tokens.AccessToken);

            using var response = await authClient.PatchAsJsonAsync("/api/Auth/password",
                new PasswordUpdateRequestDto
                {
                    OldPassword = request.Password,
                    NewPassword = newPassword
                });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var relogin = await ApiIntegrationTestHelper.LoginAsync(_client, request.Email, newPassword);
            Assert.NotNull(relogin);
            Assert.True(relogin.Success);
        }
        finally
        {
            await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, request.Email);
        }
    }
}