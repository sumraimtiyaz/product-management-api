using ProductManagement.Application.DTOs;

namespace ProductManagement.Application.Interfaces;

public interface ITokenService
{
    AccessToken CreateAccessToken(AuthenticatedUser user);

    GeneratedRefreshToken CreateRefreshToken();

    /// <summary>Hashes a refresh token value for storage and lookup. The same input always yields the same hash.</summary>
    string HashToken(string token);
}
