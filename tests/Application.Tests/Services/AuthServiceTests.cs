using Moq;
using ProductManagement.Application.DTOs;
using ProductManagement.Application.Interfaces;
using ProductManagement.Application.Services;
using ProductManagement.Application.Tests.Support;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Enums;
using ProductManagement.Domain.Exceptions;

namespace ProductManagement.Application.Tests.Services;

public class AuthServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly AuthenticatedUser Alice = new("user-1", "alice@example.com", new[] { RoleNames.User });

    private readonly Mock<IIdentityService> _identity = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _tokens.Setup(tokens => tokens.HashToken(It.IsAny<string>())).Returns((string value) => $"hash({value})");
        _tokens.Setup(tokens => tokens.CreateAccessToken(It.IsAny<AuthenticatedUser>()))
            .Returns(new AccessToken("access-token", Now.UtcDateTime.AddMinutes(15)));
        _tokens.Setup(tokens => tokens.CreateRefreshToken())
            .Returns(new GeneratedRefreshToken("new-refresh", "hash(new-refresh)", Now.UtcDateTime.AddDays(7)));

        _service = new AuthService(
            _identity.Object, _tokens.Object, _refreshTokens.Object, _unitOfWork.Object, new FixedTimeProvider(Now));
    }

    private RefreshToken StoredToken(string value, int createdDaysAgo = 1, int validForDays = 6)
    {
        var token = RefreshToken.Create(
            Alice.Id, $"hash({value})", Now.UtcDateTime.AddDays(-createdDaysAgo), Now.UtcDateTime.AddDays(validForDays));
        _refreshTokens.Setup(repository => repository.GetByHashAsync($"hash({value})", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);
        return token;
    }

    [Fact]
    public async Task Register_creates_a_user_in_the_User_role_and_starts_a_session()
    {
        _identity.Setup(identity => identity.CreateUserAsync(
                Alice.Email, "Str0ng!Passw0rd", RoleNames.User, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Alice);

        var response = await _service.RegisterAsync(new RegisterRequest(Alice.Email, "Str0ng!Passw0rd"), CancellationToken.None);

        Assert.Equal("access-token", response.AccessToken);
        Assert.Equal("new-refresh", response.RefreshToken);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_stores_only_the_hash_of_the_refresh_token()
    {
        _identity.Setup(identity => identity.ValidateCredentialsAsync(Alice.Email, "pw", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Alice);

        var response = await _service.LoginAsync(new LoginRequest(Alice.Email, "pw"), CancellationToken.None);

        Assert.Equal("new-refresh", response.RefreshToken);
        _refreshTokens.Verify(
            repository => repository.Add(It.Is<RefreshToken>(token =>
                token.TokenHash == "hash(new-refresh)" && token.UserId == Alice.Id)),
            Times.Once);
    }

    [Fact]
    public async Task Login_throws_AuthenticationFailed_for_bad_credentials_and_issues_nothing()
    {
        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => _service.LoginAsync(new LoginRequest(Alice.Email, "wrong"), CancellationToken.None));

        _tokens.Verify(tokens => tokens.CreateAccessToken(It.IsAny<AuthenticatedUser>()), Times.Never);
        _refreshTokens.Verify(repository => repository.Add(It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact]
    public async Task Refresh_rotates_the_token_revoking_the_old_one_and_linking_the_new_one()
    {
        var stored = StoredToken("old-refresh");
        _identity.Setup(identity => identity.FindByIdAsync(Alice.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Alice);

        var response = await _service.RefreshAsync(new RefreshTokenRequest("old-refresh"), CancellationToken.None);

        Assert.Equal("new-refresh", response.RefreshToken);
        Assert.True(stored.IsRevoked);
        Assert.Equal("hash(new-refresh)", stored.ReplacedByTokenHash);
        _refreshTokens.Verify(
            repository => repository.Add(It.Is<RefreshToken>(token => token.TokenHash == "hash(new-refresh)")),
            Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Refresh_rejects_an_unknown_token()
    {
        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => _service.RefreshAsync(new RefreshTokenRequest("unknown"), CancellationToken.None));

        _refreshTokens.Verify(repository => repository.Add(It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact]
    public async Task Refresh_rejects_an_expired_token()
    {
        StoredToken("old-refresh", createdDaysAgo: 8, validForDays: -1);

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => _service.RefreshAsync(new RefreshTokenRequest("old-refresh"), CancellationToken.None));

        _refreshTokens.Verify(repository => repository.Add(It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact]
    public async Task Refresh_rejects_a_token_whose_user_no_longer_exists()
    {
        StoredToken("old-refresh");

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => _service.RefreshAsync(new RefreshTokenRequest("old-refresh"), CancellationToken.None));
    }

    [Fact]
    public async Task Reusing_a_rotated_token_revokes_every_active_session_of_the_user()
    {
        var reused = StoredToken("old-refresh");
        reused.Revoke(Now.UtcDateTime.AddHours(-1), "hash(newer)");

        var otherSession = RefreshToken.Create(Alice.Id, "hash(other)", Now.UtcDateTime.AddDays(-1), Now.UtcDateTime.AddDays(6));
        var latestSession = RefreshToken.Create(Alice.Id, "hash(newer)", Now.UtcDateTime.AddHours(-1), Now.UtcDateTime.AddDays(7));
        IReadOnlyList<RefreshToken> active = new[] { otherSession, latestSession };
        _refreshTokens.Setup(repository => repository.GetActiveByUserAsync(Alice.Id, Now.UtcDateTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync(active);

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => _service.RefreshAsync(new RefreshTokenRequest("old-refresh"), CancellationToken.None));

        Assert.True(otherSession.IsRevoked);
        Assert.True(latestSession.IsRevoked);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokens.Verify(repository => repository.Add(It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact]
    public async Task Revoke_deactivates_an_active_token()
    {
        var stored = StoredToken("old-refresh");

        await _service.RevokeAsync(new RefreshTokenRequest("old-refresh"), CancellationToken.None);

        Assert.True(stored.IsRevoked);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Revoke_ignores_unknown_and_already_revoked_tokens()
    {
        var revoked = StoredToken("revoked-refresh");
        revoked.Revoke(Now.UtcDateTime.AddHours(-1));

        await _service.RevokeAsync(new RefreshTokenRequest("unknown"), CancellationToken.None);
        await _service.RevokeAsync(new RefreshTokenRequest("revoked-refresh"), CancellationToken.None);

        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
