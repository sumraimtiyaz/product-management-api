using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using ProductManagement.Application.DTOs;
using ProductManagement.Application.Interfaces;
using ProductManagement.Domain.Exceptions;

namespace ProductManagement.Infrastructure.Identity;

public sealed class IdentityService(UserManager<ApplicationUser> userManager) : IIdentityService
{
    private static readonly string[] DuplicateUserErrorCodes = ["DuplicateUserName", "DuplicateEmail"];

    public async Task<AuthenticatedUser> CreateUserAsync(
        string email, string password, string role, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser { UserName = email, Email = email };

        ThrowIfFailed(await userManager.CreateAsync(user, password));
        ThrowIfFailed(await userManager.AddToRoleAsync(user, role));

        return new AuthenticatedUser(user.Id, email, [role]);
    }

    public async Task<AuthenticatedUser?> ValidateCredentialsAsync(
        string email, string password, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || await userManager.IsLockedOutAsync(user))
        {
            return null;
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            await userManager.AccessFailedAsync(user);
            return null;
        }

        await userManager.ResetAccessFailedCountAsync(user);
        return await ToAuthenticatedUserAsync(user);
    }

    public async Task<AuthenticatedUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user is null ? null : await ToAuthenticatedUserAsync(user);
    }

    private async Task<AuthenticatedUser> ToAuthenticatedUserAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new AuthenticatedUser(user.Id, user.Email!, roles.ToList());
    }

    private static void ThrowIfFailed(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        if (result.Errors.Any(error => DuplicateUserErrorCodes.Contains(error.Code)))
        {
            throw new ConflictException("A user with this email address already exists.");
        }

        throw new ValidationException(result.Errors.Select(error => new ValidationFailure(error.Code, error.Description)));
    }
}
