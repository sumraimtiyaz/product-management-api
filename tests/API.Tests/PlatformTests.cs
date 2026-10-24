using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProductManagement.Api.Tests.Support;

namespace ProductManagement.Api.Tests;

/// <summary>Cross-cutting behaviour: documentation, security headers, CORS, compression and error shape.</summary>
public sealed class PlatformTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task The_openapi_document_describes_the_api_and_its_bearer_scheme_without_authentication()
    {
        var response = await factory.CreateClient().GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await response.Content.ReadFromJsonAsync<JsonElement>();
        var paths = document.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/v1/products", out _));
        Assert.True(paths.TryGetProperty("/api/v1/products/{productId}/items", out _));
        Assert.True(paths.TryGetProperty("/api/v1/auth/login", out _));
        Assert.True(document.GetProperty("components").GetProperty("securitySchemes").TryGetProperty("Bearer", out _));
    }

    [Fact]
    public async Task The_swagger_ui_is_served_without_authentication()
    {
        var response = await factory.CreateClient().GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Responses_carry_security_headers()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/products");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Contains("default-src 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.False(response.Headers.Contains("Server"));
    }

    [Fact]
    public async Task Unknown_routes_return_a_problem_response_once_signed_in()
    {
        var user = await factory.CreateUserAsync();

        var response = await user.Client.GetAsync("/api/v1/does-not-exist");

        await response.ReadProblemAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Unknown_routes_do_not_reveal_themselves_to_anonymous_callers()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/does-not-exist");

        await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Cross_origin_requests_are_not_allowed_unless_an_origin_is_configured()
    {
        var user = await factory.CreateUserAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/products");
        request.Headers.Add("Origin", "https://evil.example");

        var response = await user.Client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Responses_are_compressed_when_the_client_accepts_gzip()
    {
        var user = await factory.CreateUserAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/products");
        request.Headers.Add("Accept-Encoding", "gzip");

        var response = await user.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("gzip", response.Content.Headers.ContentEncoding);
    }

    [Fact]
    public async Task Version_one_is_reported_on_responses()
    {
        var user = await factory.CreateUserAsync();

        var response = await user.Client.GetAsync("/api/v1/products");

        Assert.Equal("1.0", response.Headers.GetValues("api-supported-versions").Single());
    }
}
