using ProductManagement.Application.DTOs;

namespace ProductManagement.Application.Interfaces;

/// <summary>User accounts and credentials, hiding how and where users are stored.</summary>
public interface IIdentityService
{
    /// <exception cref="Domain.Exceptions.ConflictException">The email is already registered.</exception>
    /// <exception cref="FluentValidation.ValidationException">The password does not meet the password policy.</exception>
    Task<AuthenticatedUser> CreateUserAsync(
        string email, string password, string role, CancellationToken cancellationToken);

    /// <summary>Returns the user when the credentials are valid, or <c>null</c> otherwise (including when locked out).</summary>
    Task<AuthenticatedUser?> ValidateCredentialsAsync(
        string email, string password, CancellationToken cancellationToken);

    Task<AuthenticatedUser?> FindByIdAsync(string userId, CancellationToken cancellationToken);
}
