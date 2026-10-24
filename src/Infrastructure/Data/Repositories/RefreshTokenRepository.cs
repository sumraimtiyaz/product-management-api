using Microsoft.EntityFrameworkCore;
using ProductManagement.Application.Interfaces;
using ProductManagement.Domain.Entities;

namespace ProductManagement.Infrastructure.Data.Repositories;

public sealed class RefreshTokenRepository(ApplicationDbContext context) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        context.RefreshTokens.FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(
        string userId, DateTime utcNow, CancellationToken cancellationToken) =>
        await context.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedOn == null && token.ExpiresOn > utcNow)
            .ToListAsync(cancellationToken);

    public void Add(RefreshToken refreshToken) => context.RefreshTokens.Add(refreshToken);
}
