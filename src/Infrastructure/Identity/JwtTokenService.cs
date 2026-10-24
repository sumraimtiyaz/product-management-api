using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProductManagement.Application.DTOs;
using ProductManagement.Application.Interfaces;

namespace ProductManagement.Infrastructure.Identity;

public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider clock) : ITokenService
{
    private const int RefreshTokenByteLength = 64;

    private readonly JwtOptions _options = options.Value;

    public AccessToken CreateAccessToken(AuthenticatedUser user)
    {
        var now = clock.GetUtcNow();
        var expiresOn = now.AddMinutes(_options.AccessTokenMinutes).UtcDateTime;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        claims.AddRange(user.Roles.Select(role => new Claim(TokenClaimTypes.Role, role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            claims,
            notBefore: now.UtcDateTime,
            expires: expiresOn,
            signingCredentials: credentials);

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresOn);
    }

    public GeneratedRefreshToken CreateRefreshToken()
    {
        var value = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(RefreshTokenByteLength));
        var expiresOn = clock.GetUtcNow().AddDays(_options.RefreshTokenDays).UtcDateTime;

        return new GeneratedRefreshToken(value, HashToken(value), expiresOn);
    }

    public string HashToken(string token) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
