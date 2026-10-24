using System.ComponentModel.DataAnnotations;

namespace ProductManagement.Infrastructure.Identity;

/// <summary>Settings for issuing and validating JWT access tokens. Validated when the application starts.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>Secret used to sign tokens (HMAC-SHA256). Never commit a real value; supply it as a secret.</summary>
    [Required, MinLength(32)]
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Access tokens are deliberately short-lived; clients renew them with a refresh token.</summary>
    [Range(1, 1440)]
    public int AccessTokenMinutes { get; set; } = 15;

    [Range(1, 90)]
    public int RefreshTokenDays { get; set; } = 7;
}
