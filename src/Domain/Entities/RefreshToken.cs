using ProductManagement.Domain.Exceptions;

namespace ProductManagement.Domain.Entities;

/// <summary>
/// A single-use refresh token. Only a hash of the token is stored, and each use
/// revokes the token and links it to its replacement (rotation).
/// </summary>
public class RefreshToken
{
    public const int TokenHashMaxLength = 128;

    // Required by EF Core; instances are created through Create.
    private RefreshToken()
    {
    }

    public int Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime CreatedOn { get; private set; }
    public DateTime ExpiresOn { get; private set; }
    public DateTime? RevokedOn { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    public bool IsRevoked => RevokedOn.HasValue;

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresOn;

    public bool IsActive(DateTime utcNow) => !IsRevoked && !IsExpired(utcNow);

    public static RefreshToken Create(string userId, string tokenHash, DateTime createdOn, DateTime expiresOn)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new DomainException("A refresh token must belong to a user.");
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new DomainException("A refresh token hash is required.");
        }

        if (expiresOn <= createdOn)
        {
            throw new DomainException("A refresh token must expire after it is created.");
        }

        return new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            CreatedOn = createdOn,
            ExpiresOn = expiresOn
        };
    }

    public void Revoke(DateTime utcNow, string? replacedByTokenHash = null)
    {
        if (IsRevoked)
        {
            return;
        }

        RevokedOn = utcNow;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
