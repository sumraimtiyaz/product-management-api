namespace ProductManagement.Application.DTOs;

/// <summary>Payload for registering a new user account.</summary>
public sealed record RegisterRequest(string Email, string Password);

/// <summary>Payload for signing in.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>Payload for exchanging or revoking a refresh token.</summary>
public sealed record RefreshTokenRequest(string RefreshToken);

/// <summary>A new access token and refresh token pair.</summary>
/// <param name="AccessToken">Short-lived JWT to send as a bearer token.</param>
/// <param name="AccessTokenExpiresOn">UTC expiry of the access token.</param>
/// <param name="RefreshToken">Single-use token to obtain the next pair. Store it securely.</param>
/// <param name="RefreshTokenExpiresOn">UTC expiry of the refresh token.</param>
public sealed record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresOn,
    string RefreshToken,
    DateTime RefreshTokenExpiresOn);

/// <summary>The identity of a signed-in user, independent of how users are stored.</summary>
public sealed record AuthenticatedUser(string Id, string Email, IReadOnlyList<string> Roles);

/// <summary>A signed access token and its expiry.</summary>
public sealed record AccessToken(string Value, DateTime ExpiresOn);

/// <summary>A freshly generated refresh token: the secret value for the client and the hash to store.</summary>
public sealed record GeneratedRefreshToken(string Value, string Hash, DateTime ExpiresOn);
