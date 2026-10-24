using ProductManagement.Domain.Entities;
using ProductManagement.Infrastructure.Data.Repositories;
using ProductManagement.Infrastructure.Identity;
using ProductManagement.Infrastructure.Tests.Support;

namespace ProductManagement.Infrastructure.Tests.Data;

public sealed class RefreshTokenRepositoryTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly TestDatabase _db = new();

    public RefreshTokenRepositoryTests()
    {
        using var context = _db.CreateContext();
        context.Users.AddRange(
            new ApplicationUser { Id = "user-1", UserName = "alice@example.com" },
            new ApplicationUser { Id = "user-2", UserName = "bob@example.com" });
        context.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetByHash_finds_the_stored_token_as_a_tracked_entity()
    {
        await using (var context = _db.CreateContext())
        {
            new RefreshTokenRepository(context).Add(RefreshToken.Create("user-1", "hash-1", Now, Now.AddDays(7)));
            await context.SaveChangesAsync();
        }

        await using var readContext = _db.CreateContext();
        var token = await new RefreshTokenRepository(readContext).GetByHashAsync("hash-1", CancellationToken.None);

        Assert.NotNull(token);
        Assert.Equal("user-1", token.UserId);
        Assert.Single(readContext.ChangeTracker.Entries<RefreshToken>());
        Assert.Null(await new RefreshTokenRepository(readContext).GetByHashAsync("missing", CancellationToken.None));
    }

    [Fact]
    public async Task GetActiveByUser_returns_only_unrevoked_unexpired_tokens_of_that_user()
    {
        await using (var context = _db.CreateContext())
        {
            var revoked = RefreshToken.Create("user-1", "revoked", Now.AddDays(-2), Now.AddDays(5));
            revoked.Revoke(Now.AddDays(-1));

            context.RefreshTokens.AddRange(
                RefreshToken.Create("user-1", "active", Now.AddDays(-1), Now.AddDays(6)),
                revoked,
                RefreshToken.Create("user-1", "expired", Now.AddDays(-9), Now.AddDays(-2)),
                RefreshToken.Create("user-2", "other-user", Now.AddDays(-1), Now.AddDays(6)));
            await context.SaveChangesAsync();
        }

        await using var readContext = _db.CreateContext();
        var active = await new RefreshTokenRepository(readContext).GetActiveByUserAsync("user-1", Now, CancellationToken.None);

        Assert.Equal(new[] { "active" }, active.Select(token => token.TokenHash).ToArray());
    }

    [Fact]
    public async Task Two_tokens_cannot_share_a_hash()
    {
        await using var context = _db.CreateContext();
        context.RefreshTokens.AddRange(
            RefreshToken.Create("user-1", "same", Now, Now.AddDays(7)),
            RefreshToken.Create("user-2", "same", Now, Now.AddDays(7)));

        await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(() => context.SaveChangesAsync());
    }
}
