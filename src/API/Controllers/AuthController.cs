using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductManagement.Application.DTOs;
using ProductManagement.Application.Interfaces;

namespace ProductManagement.Api.Controllers;

/// <summary>
/// Sign-up, sign-in and token management. Access tokens are short-lived JWTs; each refresh token is single-use
/// and is replaced by a new one every time it is exchanged.
/// </summary>
[ApiVersion("1.0")]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController(IAuthService auth) : ApiControllerBase
{
    /// <summary>Registers a user with the <c>User</c> role and signs them in.</summary>
    /// <param name="request">Email and password.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await auth.RegisterAsync(request, cancellationToken));

    /// <summary>Signs a user in and returns an access token and a refresh token.</summary>
    /// <param name="request">Email and password.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken) =>
        Ok(await auth.LoginAsync(request, cancellationToken));

    /// <summary>
    /// Exchanges a refresh token for a new access token and refresh token (rotation). Presenting a token that was
    /// already used revokes every session of that user.
    /// </summary>
    /// <param name="request">The refresh token.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Refresh(
        RefreshTokenRequest request, CancellationToken cancellationToken) =>
        Ok(await auth.RefreshAsync(request, cancellationToken));

    /// <summary>Revokes a refresh token (sign out). Unknown or already revoked tokens are ignored.</summary>
    /// <param name="request">The refresh token.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Revoke(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await auth.RevokeAsync(request, cancellationToken);
        return NoContent();
    }
}
