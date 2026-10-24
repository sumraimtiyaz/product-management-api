using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Exceptions;

namespace ProductManagement.Application.Tests.Domain;

public class RefreshTokenTests
{
    private static readonly DateTime Created = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Expires = Created.AddDays(7);

    [Fact]
    public void A_new_token_is_active_until_it_expires()
    {
        var token = RefreshToken.Create("user-1", "hash", Created, Expires);

        Assert.True(token.IsActive(Created.AddDays(1)));
        Assert.False(token.IsActive(Expires));
        Assert.True(token.IsExpired(Expires.AddSeconds(1)));
    }

    [Fact]
    public void Revoke_deactivates_the_token_and_records_its_replacement()
    {
        var token = RefreshToken.Create("user-1", "hash", Created, Expires);
        var revokedOn = Created.AddDays(1);

        token.Revoke(revokedOn, "replacement-hash");

        Assert.True(token.IsRevoked);
        Assert.False(token.IsActive(revokedOn));
        Assert.Equal(revokedOn, token.RevokedOn);
        Assert.Equal("replacement-hash", token.ReplacedByTokenHash);
    }

    [Fact]
    public void Revoking_twice_keeps_the_first_revocation()
    {
        var token = RefreshToken.Create("user-1", "hash", Created, Expires);
        token.Revoke(Created.AddDays(1), "first");

        token.Revoke(Created.AddDays(2), "second");

        Assert.Equal(Created.AddDays(1), token.RevokedOn);
        Assert.Equal("first", token.ReplacedByTokenHash);
    }

    [Theory]
    [InlineData("", "hash")]
    [InlineData("user-1", "")]
    public void Create_requires_a_user_and_a_hash(string userId, string tokenHash)
    {
        Assert.Throws<DomainException>(() => RefreshToken.Create(userId, tokenHash, Created, Expires));
    }

    [Fact]
    public void Create_requires_the_expiry_to_be_after_creation()
    {
        Assert.Throws<DomainException>(() => RefreshToken.Create("user-1", "hash", Created, Created));
    }
}
