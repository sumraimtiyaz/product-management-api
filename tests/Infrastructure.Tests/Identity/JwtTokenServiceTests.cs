using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ProductManagement.Application.DTOs;
using ProductManagement.Infrastructure.Identity;
using ProductManagement.Infrastructure.Tests.Support;

namespace ProductManagement.Infrastructure.Tests.Identity;

public class JwtTokenServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly AuthenticatedUser Alice = new("user-1", "alice@example.com", new[] { "User", "Admin" });

    private readonly JwtOptions _options = new()
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        SigningKey = new string('k', 48),
        AccessTokenMinutes = 15,
        RefreshTokenDays = 7
    };

    private JwtTokenService CreateService(TimeProvider? clock = null) =>
        new(Microsoft.Extensions.Options.Options.Create(_options), clock ?? new TestClock(Now));

    [Fact]
    public void Access_token_carries_the_subject_email_name_and_every_role()
    {
        var token = CreateService().CreateAccessToken(Alice);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Value);

        Assert.Equal("user-1", jwt.Subject);
        Assert.Equal("alice@example.com", jwt.Claims.Single(claim => claim.Type == "email").Value);
        Assert.Equal("alice@example.com", jwt.Claims.Single(claim => claim.Type == "name").Value);
        Assert.Equal(
            new[] { "Admin", "User" },
            jwt.Claims.Where(claim => claim.Type == TokenClaimTypes.Role).Select(claim => claim.Value).Order().ToArray());
        Assert.Equal("test-issuer", jwt.Issuer);
        Assert.Contains("test-audience", jwt.Audiences);
    }

    [Fact]
    public void Access_token_is_short_lived()
    {
        var token = CreateService().CreateAccessToken(Alice);

        Assert.Equal(Now.AddMinutes(15).UtcDateTime, token.ExpiresOn);
        Assert.Equal(token.ExpiresOn, new JwtSecurityTokenHandler().ReadJwtToken(token.Value).ValidTo, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Access_token_is_valid_for_the_signing_key_and_rejected_for_another()
    {
        var service = CreateService(TimeProvider.System);
        var token = service.CreateAccessToken(Alice);
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };

        TokenValidationParameters Parameters(string key) => new()
        {
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            NameClaimType = "name",
            RoleClaimType = TokenClaimTypes.Role
        };

        var principal = handler.ValidateToken(token.Value, Parameters(_options.SigningKey), out _);

        Assert.Equal("alice@example.com", principal.Identity!.Name);
        Assert.True(principal.IsInRole("Admin"));
        Assert.ThrowsAny<SecurityTokenException>(
            () => handler.ValidateToken(token.Value, Parameters(new string('x', 48)), out _));
    }

    [Fact]
    public void Two_access_tokens_for_the_same_user_are_different()
    {
        var service = CreateService();

        Assert.NotEqual(service.CreateAccessToken(Alice).Value, service.CreateAccessToken(Alice).Value);
    }

    [Fact]
    public void Refresh_token_is_random_url_safe_hashed_and_expires_after_the_configured_days()
    {
        var service = CreateService();

        var first = service.CreateRefreshToken();
        var second = service.CreateRefreshToken();

        Assert.NotEqual(first.Value, second.Value);
        Assert.DoesNotContain(first.Value, character => character is '+' or '/' or '=');
        Assert.True(first.Value.Length >= 64);
        Assert.Equal(service.HashToken(first.Value), first.Hash);
        Assert.NotEqual(first.Value, first.Hash);
        Assert.Equal(Now.AddDays(7).UtcDateTime, first.ExpiresOn);
    }

    [Fact]
    public void Hashing_is_deterministic_and_input_sensitive()
    {
        var service = CreateService();

        Assert.Equal(service.HashToken("abc"), service.HashToken("abc"));
        Assert.NotEqual(service.HashToken("abc"), service.HashToken("abd"));
    }
}
