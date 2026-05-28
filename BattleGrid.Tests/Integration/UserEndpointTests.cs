using System.Net;
using System.Net.Http.Json;
using BattleGrid.Contracts.RequestDtos;
using BattleGrid.Contracts.ResponseDtos;

namespace BattleGrid.Tests.Integration;

public sealed class UserEndpointTests : IClassFixture<BattleGridApiFactory>, IAsyncLifetime
{
    private readonly BattleGridApiFactory _factory;
    private HttpClient _client = null!;
    private bool _databaseAvailable;

    public UserEndpointTests(BattleGridApiFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        _databaseAvailable = await _factory.CanConnectToDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetByLoginInfo_WithoutAuth_ReturnsUser()
    {
        if (!_databaseAvailable)
            return;

        var request = ApiIntegrationTestHelper.CreateUniqueRegisterRequest();
        try
        {
            await ApiIntegrationTestHelper.RegisterUserForSetupAsync(_client, request);

            var user = await _client.GetFromJsonAsync<UserResponseDto>(
                $"/api/User/{Uri.EscapeDataString(request.Email)}");

            Assert.NotNull(user);
            Assert.Equal(request.UserName, user.UserName);
            Assert.Equal(request.Email, user.Email);
        }
        finally
        {
            await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, request.Email);
        }
    }

    [Fact]
    public async Task GetByLoginInfo_UnknownUser_ReturnsNotFound()
    {
        if (!_databaseAvailable)
            return;

        using var response = await _client.GetAsync(
            $"/api/User/{Uri.EscapeDataString("nobody@battlegrid.test")}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithAuth_ReturnsCurrentUser()
    {
        if (!_databaseAvailable)
            return;

        TestUserSession? session = null;
        try
        {
            session = await ApiIntegrationTestHelper.CreateLoggedInUserAsync(_client, _factory);
            using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(
                _factory, session.Tokens!.AccessToken);

            var me = await authClient.GetFromJsonAsync<UserResponseDto>("/api/User/me");

            Assert.NotNull(me);
            Assert.Equal(session.Email, me.Email);
            Assert.Equal(session.UserName, me.UserName);
        }
        finally
        {
            if (session is not null)
                await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, session.Email);
        }
    }

    [Fact]
    public async Task GetMe_WithoutAuth_ReturnsUnauthorized()
    {
        if (!_databaseAvailable)
            return;

        using var response = await _client.GetAsync("/api/User/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserById_WithoutAuth_ReturnsUnauthorized()
    {
        if (!_databaseAvailable)
            return;

        using var response = await _client.GetAsync("/api/User/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserById_WithAuth_ReturnsUser()
    {
        if (!_databaseAvailable)
            return;

        TestUserSession? session = null;
        try
        {
            session = await ApiIntegrationTestHelper.CreateRegisteredUserAsync(_client);
            Assert.NotNull(session.Profile);

            var tokens = await ApiIntegrationTestHelper.LoginAsync(
                _client, session.Email, session.Password);
            Assert.NotNull(tokens);

            using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(
                _factory, tokens.AccessToken);

            var user = await authClient.GetFromJsonAsync<UserResponseDto>(
                $"/api/User/{session.Profile.UserID}");

            Assert.NotNull(user);
            Assert.Equal(session.Profile.UserID, user.UserID);
        }
        finally
        {
            if (session is not null)
                await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, session.Email);
        }
    }

    [Fact]
    public async Task GetAllUsers_AsNonAdmin_ReturnsForbidden()
    {
        if (!_databaseAvailable)
            return;

        TestUserSession? session = null;
        try
        {
            session = await ApiIntegrationTestHelper.CreateLoggedInUserAsync(_client, _factory);
            using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(
                _factory, session.Tokens!.AccessToken);

            using var response = await authClient.GetAsync("/api/User/all");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        finally
        {
            if (session is not null)
                await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, session.Email);
        }
    }

    [Fact]
    public async Task GetAllUsers_WithoutAuth_ReturnsUnauthorized()
    {
        if (!_databaseAvailable)
            return;

        using var response = await _client.GetAsync("/api/User/all");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAllUsers_AsAdmin_ReturnsOk()
    {
        if (!_databaseAvailable)
            return;

        TestUserSession? session = null;
        try
        {
            session = await ApiIntegrationTestHelper.CreateLoggedInUserAsync(_client, _factory, asAdmin: true);
            using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(
                _factory, session.Tokens!.AccessToken);

            using var response = await authClient.GetAsync("/api/User/all?page=1&pageSize=20");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var page = await response.Content.ReadFromJsonAsync<PagedUsersResponseDto>();
            Assert.NotNull(page);
            Assert.NotEmpty(page.Items);
            Assert.Equal(1, page.Page);
            Assert.Equal(20, page.PageSize);
            Assert.True(page.TotalCount >= page.Items.Count);
        }
        finally
        {
            if (session is not null)
                await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, session.Email);
        }
    }

    [Fact]
    public async Task GetBanStatus_WithAuth_ReturnsOk()
    {
        if (!_databaseAvailable)
            return;

        TestUserSession? session = null;
        try
        {
            session = await ApiIntegrationTestHelper.CreateLoggedInUserAsync(_client, _factory);
            using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(
                _factory, session.Tokens!.AccessToken);

            var status = await authClient.GetFromJsonAsync<UserBanStatusResponseDto>("/api/User/ban-status");

            Assert.NotNull(status);
            Assert.False(status.IsBanned);
        }
        finally
        {
            if (session is not null)
                await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, session.Email);
        }
    }

    [Fact]
    public async Task GetBanStatus_WithoutAuth_ReturnsUnauthorized()
    {
        if (!_databaseAvailable)
            return;

        using var response = await _client.GetAsync("/api/User/ban-status");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentSeasonStats_WithAuth_ReturnsOk()
    {
        if (!_databaseAvailable)
            return;

        TestUserSession? session = null;
        try
        {
            session = await ApiIntegrationTestHelper.CreateLoggedInUserAsync(_client, _factory);
            using var authClient = ApiIntegrationTestHelper.CreateAuthenticatedClient(
                _factory, session.Tokens!.AccessToken);

            var stats = await authClient.GetFromJsonAsync<PlayerSeasonStatsResponseDto>(
                "/api/User/stats/current-season");

            Assert.NotNull(stats);
            Assert.True(stats.SeasonNo >= 1);
            Assert.True(stats.Rating >= 0);
        }
        finally
        {
            if (session is not null)
                await ApiIntegrationTestHelper.CleanupTestUserAsync(_factory, session.Email);
        }
    }

    [Fact]
    public async Task GetCurrentSeasonStats_WithoutAuth_ReturnsUnauthorized()
    {
        if (!_databaseAvailable)
            return;

        using var response = await _client.GetAsync("/api/User/stats/current-season");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangeEmail_WithoutAuth_ReturnsUnauthorized()
    {
        if (!_databaseAvailable)
            return;

        using var response = await _client.PatchAsJsonAsync("/api/User/email", new ChangeEmailRequestDto
        {
            NewEmail = "x@test.com",
            Password = "x"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangeUsername_WithoutAuth_ReturnsUnauthorized()
    {
        if (!_databaseAvailable)
            return;

        using var response = await _client.PatchAsJsonAsync("/api/User/username", new ChangeUsernameRequestDto
        {
            NewUserName = "x_name",
            Password = "x"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateAccount_WithoutAuth_ReturnsUnauthorized()
    {
        if (!_databaseAvailable)
            return;

        using var response = await _client.PatchAsJsonAsync("/api/User/deactivate", new DeactivateAccountRequestDto
        {
            Password = "x"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}