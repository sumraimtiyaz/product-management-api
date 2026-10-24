using ProductManagement.Application.DTOs;
using ProductManagement.Application.Interfaces;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Enums;
using ProductManagement.Domain.Exceptions;

namespace ProductManagement.Application.Services;

public sealed class AuthService(
    IIdentityService identity,
    ITokenService tokens,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : IAuthService
{
    private const string InvalidCredentials = "Invalid email or password.";
    private const string InvalidRefreshToken = "The refresh token is invalid, expired or has been revoked.";

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var user = await identity.CreateUserAsync(request.Email, request.Password, RoleNames.User, cancellationToken);

        return await StartSessionAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await identity.ValidateCredentialsAsync(request.Email, request.Password, cancellationToken)
            ?? throw new AuthenticationFailedException(InvalidCredentials);

        return await StartSessionAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var now = UtcNow();
        var stored = await refreshTokens.GetByHashAsync(tokens.HashToken(request.RefreshToken), cancellationToken)
            ?? throw new AuthenticationFailedException(InvalidRefreshToken);

        if (stored.IsRevoked)
        {
            // A rotated token was presented again, so it may have been stolen: end every session of that user.
            await RevokeAllSessionsAsync(stored.UserId, now, cancellationToken);
            throw new AuthenticationFailedException(InvalidRefreshToken);
        }

        if (stored.IsExpired(now))
        {
            throw new AuthenticationFailedException(InvalidRefreshToken);
        }

        var user = await identity.FindByIdAsync(stored.UserId, cancellationToken)
            ?? throw new AuthenticationFailedException(InvalidRefreshToken);

        var (response, replacement) = CreateSession(user, now);
        stored.Revoke(now, replacement.TokenHash);
        refreshTokens.Add(replacement);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task RevokeAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var now = UtcNow();
        var stored = await refreshTokens.GetByHashAsync(tokens.HashToken(request.RefreshToken), cancellationToken);

        if (stored is null || !stored.IsActive(now))
        {
            return;
        }

        stored.Revoke(now);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthResponse> StartSessionAsync(AuthenticatedUser user, CancellationToken cancellationToken)
    {
        var (response, refreshToken) = CreateSession(user, UtcNow());

        refreshTokens.Add(refreshToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }

    private (AuthResponse Response, RefreshToken RefreshToken) CreateSession(AuthenticatedUser user, DateTime now)
    {
        var accessToken = tokens.CreateAccessToken(user);
        var refreshToken = tokens.CreateRefreshToken();

        var stored = RefreshToken.Create(user.Id, refreshToken.Hash, now, refreshToken.ExpiresOn);
        var response = new AuthResponse(
            accessToken.Value, accessToken.ExpiresOn, refreshToken.Value, refreshToken.ExpiresOn);

        return (response, stored);
    }

    private async Task RevokeAllSessionsAsync(string userId, DateTime now, CancellationToken cancellationToken)
    {
        foreach (var token in await refreshTokens.GetActiveByUserAsync(userId, now, cancellationToken))
        {
            token.Revoke(now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private DateTime UtcNow() => clock.GetUtcNow().UtcDateTime;
}
