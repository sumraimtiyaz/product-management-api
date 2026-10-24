using ProductManagement.Application.DTOs;

namespace ProductManagement.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    /// <summary>Exchanges a refresh token for a new pair. The presented token is revoked (rotation).</summary>
    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);

    /// <summary>Revokes a refresh token. Unknown or already inactive tokens are ignored.</summary>
    Task RevokeAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
}
