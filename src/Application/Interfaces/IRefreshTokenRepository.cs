using ProductManagement.Domain.Entities;

namespace ProductManagement.Application.Interfaces;

public interface IRefreshTokenRepository
{
    /// <summary>Finds a stored token by the hash of its value. The returned entity is tracked.</summary>
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>Lists a user's tokens that are neither revoked nor expired. The returned entities are tracked.</summary>
    Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(
        string userId, DateTime utcNow, CancellationToken cancellationToken);

    void Add(RefreshToken refreshToken);
}
