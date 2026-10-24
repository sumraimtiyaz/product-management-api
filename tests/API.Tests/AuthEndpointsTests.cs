using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ProductManagement.Api.Tests.Support;
using ProductManagement.Application.DTOs;

namespace ProductManagement.Api.Tests;

public sealed class AuthEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Password = ApiFactory.UserPassword;

    private static string NewEmail() => $"user-{Guid.NewGuid():N}@test.local";

    private Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email, string password = Password) =>
        client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, password));

    private async Task<AuthResponse> RegisterAndReadAsync(HttpClient client)
    {
        var response = await RegisterAsync(client, NewEmail());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadAsync<AuthResponse>();
    }

    private static Task<HttpResponseMessage> RefreshAsync(HttpClient client, string refreshToken) =>
        client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(refreshToken));

    [Fact]
    public async Task Register_returns_a_token_pair_that_grants_access()
    {
        var client = factory.CreateClient();

        var tokens = await RegisterAndReadAsync(client);

        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
        Assert.True(tokens.AccessTokenExpiresOn > DateTime.UtcNow);
        Assert.True(tokens.RefreshTokenExpiresOn > tokens.AccessTokenExpiresOn);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/products")).StatusCode);
    }

    [Fact]
    public async Task Register_with_an_email_that_is_taken_returns_409()
    {
        var client = factory.CreateClient();
        var email = NewEmail();
        await RegisterAsync(client, email);

        var duplicate = await RegisterAsync(client, email.ToUpperInvariant());

        await duplicate.ReadProblemAsync(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_with_invalid_input_returns_400_listing_each_field_in_camel_case()
    {
        var client = factory.CreateClient();

        var response = await RegisterAsync(client, "not-an-email", "short");

        var problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        var errors = problem.GetProperty("errors");
        Assert.True(errors.TryGetProperty("email", out _));
        Assert.True(errors.TryGetProperty("password", out _));
    }

    [Fact]
    public async Task Register_with_a_password_that_breaks_the_policy_returns_400()
    {
        var client = factory.CreateClient();

        var response = await RegisterAsync(client, NewEmail(), "alllowercase");

        var problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.True(problem.GetProperty("errors").TryGetProperty("passwordRequiresUpper", out _));
    }

    [Fact]
    public async Task Login_with_valid_credentials_returns_tokens()
    {
        var client = factory.CreateClient();
        var email = NewEmail();
        await RegisterAsync(client, email);

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, Password));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace((await response.ReadAsync<AuthResponse>()).AccessToken));
    }

    [Fact]
    public async Task Login_with_a_wrong_password_or_unknown_user_returns_401()
    {
        var client = factory.CreateClient();
        var email = NewEmail();
        await RegisterAsync(client, email);

        var wrongPassword = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "Wr0ng!Passw0rd"));
        var unknownUser = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(NewEmail(), Password));

        await wrongPassword.ReadProblemAsync(HttpStatusCode.Unauthorized);
        await unknownUser.ReadProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Repeated_failed_logins_lock_the_account()
    {
        var client = factory.CreateClient();
        var email = NewEmail();
        await RegisterAsync(client, email);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "Wr0ng!Passw0rd"));
        }

        var withCorrectPassword = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, Password));

        await withCorrectPassword.ReadProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_with_missing_fields_returns_400()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("", ""));

        await response.ReadProblemAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_issues_a_new_pair_and_the_new_access_token_works()
    {
        var client = factory.CreateClient();
        var original = await RegisterAndReadAsync(client);

        var response = await RefreshAsync(client, original.RefreshToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rotated = await response.ReadAsync<AuthResponse>();
        Assert.NotEqual(original.RefreshToken, rotated.RefreshToken);
        Assert.NotEqual(original.AccessToken, rotated.AccessToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", rotated.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/products")).StatusCode);
    }

    [Fact]
    public async Task Reusing_a_rotated_refresh_token_is_rejected_and_ends_the_whole_session()
    {
        var client = factory.CreateClient();
        var original = await RegisterAndReadAsync(client);
        var rotated = await (await RefreshAsync(client, original.RefreshToken)).ReadAsync<AuthResponse>();

        var reuse = await RefreshAsync(client, original.RefreshToken);
        var newestToken = await RefreshAsync(client, rotated.RefreshToken);

        await reuse.ReadProblemAsync(HttpStatusCode.Unauthorized);
        await newestToken.ReadProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refreshing_with_an_unknown_token_returns_401()
    {
        var client = factory.CreateClient();

        var response = await RefreshAsync(client, "this-token-was-never-issued");

        await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_revoked_refresh_token_can_no_longer_be_used()
    {
        var client = factory.CreateClient();
        var tokens = await RegisterAndReadAsync(client);

        var revoke = await client.PostAsJsonAsync("/api/v1/auth/revoke", new RefreshTokenRequest(tokens.RefreshToken));
        var refresh = await RefreshAsync(client, tokens.RefreshToken);

        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);
        await refresh.ReadProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Revoking_an_unknown_token_is_harmless()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/revoke", new RefreshTokenRequest("unknown"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task A_tampered_access_token_is_rejected()
    {
        var client = factory.CreateClient();
        var tokens = await RegisterAndReadAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken + "x");

        var response = await client.GetAsync("/api/v1/products");

        await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
    }
}
