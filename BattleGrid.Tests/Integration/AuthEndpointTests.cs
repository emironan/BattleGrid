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
            var first = await ApiIntegrationTestHelper.RegisterAndReadAsync(_client, request);
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);

            var duplicate = await ApiIntegrationTestHelper.RegisterAndReadAsync(_client, request);
            Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        }
        finally
        {
            await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, request.Email);
        }
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_ReturnsBadRequest()
    {
        if (!_databaseAvailable)
            return;

        var first = ApiIntegrationTestHelper.CreateUniqueRegisterRequest();
        var second = ApiIntegrationTestHelper.CreateUniqueRegisterRequest();
        second.UserName = first.UserName;

        try
        {
            var firstResponse = await ApiIntegrationTestHelper.RegisterAndReadAsync(_client, first);
            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

            var duplicate = await ApiIntegrationTestHelper.RegisterAndReadAsync(_client, second);
            Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        }
        finally
        {
            await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, first.Email);
            await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, second.Email);
        }
    }

    [Fact]
    public async Task Register_WithMismatchedPasswords_ReturnsBadRequest()
    {
        if (!_databaseAvailable)
            return;

        var request = ApiIntegrationTestHelper.CreateUniqueRegisterRequest();
        request.ConfirmPassword = "Different_Password_123!";

        var (status, _) = await ApiIntegrationTestHelper.RegisterAndReadAsync(_client, request);
        Assert.Equal(HttpStatusCode.BadRequest, status);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        if (!_databaseAvailable)
            return;

        var request = ApiIntegrationTestHelper.CreateUniqueRegisterRequest();
        try
        {
            await ApiIntegrationTestHelper.RegisterUserForSetupAsync(_client, request);

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
            await ApiIntegrationTestHelper.RegisterUserForSetupAsync(_client, request);

            using var response = await ApiIntegrationTestHelper.LoginPostAsync(
                _client, request.Email, "wrong-password");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, request.Email);
        }
    }

    [Fact]
    public async Task Login_WithNonExistingEmail_ReturnsUnauthorized()
    {
        if (!_databaseAvailable)
            return;

        using var response = await ApiIntegrationTestHelper.LoginPostAsync(
            _client, $"missing_{Guid.NewGuid():N}@battlegrid.test", "random-password");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithNonExistingUsername_ReturnsUnauthorized()
    {
        if (!_databaseAvailable)
            return;

        using var response = await ApiIntegrationTestHelper.LoginPostAsync(
            _client, $"missing_user_{Guid.NewGuid():N}", "random-password");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithRefreshToken_ReturnsOk()
    {
        if (!_databaseAvailable)
            return;

        var request = ApiIntegrationTestHelper.CreateUniqueRegisterRequest();
        try
        {
            await ApiIntegrationTestHelper.RegisterUserForSetupAsync(_client, request);
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
            await ApiIntegrationTestHelper.RegisterUserForSetupAsync(_client, request);
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
            NewPassword = "new",
            ConfirmNewPassword = "new"
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
            await ApiIntegrationTestHelper.RegisterUserForSetupAsync(_client, request);
            var tokens = await ApiIntegrationTestHelper.LoginAsync(
                _client, request.Email, request.Password);
            Assert.NotNull(tokens);

            using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(
                _factory, tokens.AccessToken);

            using var response = await authClient.PatchAsJsonAsync("/api/Auth/password",
                new PasswordUpdateRequestDto
                {
                    OldPassword = request.Password,
                    NewPassword = newPassword,
                    ConfirmNewPassword = newPassword
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

    [Fact]
    public async Task UpdatePassword_WithMismatchedConfirmation_ReturnsBadRequest()
    {
        if (!_databaseAvailable)
            return;

        var request = ApiIntegrationTestHelper.CreateUniqueRegisterRequest();
        try
        {
            await ApiIntegrationTestHelper.RegisterUserForSetupAsync(_client, request);
            var tokens = await ApiIntegrationTestHelper.LoginAsync(
                _client, request.Email, request.Password);
            Assert.NotNull(tokens);

            using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(
                _factory, tokens.AccessToken);

            using var response = await authClient.PatchAsJsonAsync("/api/Auth/password",
                new PasswordUpdateRequestDto
                {
                    OldPassword = request.Password,
                    NewPassword = "Mismatch_New_123!",
                    ConfirmNewPassword = "Mismatch_New_456!"
                });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        finally
        {
            await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, request.Email);
        }
    }

    [Fact]
    public async Task UpdatePassword_WithSameOldAndNew_ReturnsBadRequest()
    {
        if (!_databaseAvailable)
            return;

        var request = ApiIntegrationTestHelper.CreateUniqueRegisterRequest();
        try
        {
            await ApiIntegrationTestHelper.RegisterUserForSetupAsync(_client, request);
            var tokens = await ApiIntegrationTestHelper.LoginAsync(
                _client, request.Email, request.Password);
            Assert.NotNull(tokens);

            using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(
                _factory, tokens.AccessToken);

            using var response = await authClient.PatchAsJsonAsync("/api/Auth/password",
                new PasswordUpdateRequestDto
                {
                    OldPassword = request.Password,
                    NewPassword = request.Password,
                    ConfirmNewPassword = request.Password
                });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        finally
        {
            await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, request.Email);
        }
    }
}