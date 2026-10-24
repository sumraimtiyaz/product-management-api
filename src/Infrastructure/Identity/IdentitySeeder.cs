using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProductManagement.Domain.Enums;

namespace ProductManagement.Infrastructure.Identity;

/// <summary>Makes sure the roles exist and, when configured, that the first administrator exists.</summary>
public sealed class IdentitySeeder(
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager,
    IOptions<AdminSeedOptions> options,
    ILogger<IdentitySeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var role in Enum.GetNames<UserRole>())
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                EnsureSucceeded(await roleManager.CreateAsync(new IdentityRole(role)), $"create role '{role}'");
            }
        }

        var admin = options.Value;
        if (string.IsNullOrWhiteSpace(admin.Email) || string.IsNullOrWhiteSpace(admin.Password))
        {
            logger.LogInformation("No administrator account is configured; skipping admin seeding.");
            return;
        }

        if (await userManager.FindByEmailAsync(admin.Email) is not null)
        {
            return;
        }

        var user = new ApplicationUser { UserName = admin.Email, Email = admin.Email, EmailConfirmed = true };
        EnsureSucceeded(await userManager.CreateAsync(user, admin.Password), "create the administrator");
        EnsureSucceeded(await userManager.AddToRoleAsync(user, RoleNames.Admin), "assign the administrator role");

        logger.LogInformation("Created administrator account {Email}.", admin.Email);
    }

    private static void EnsureSucceeded(IdentityResult result, string action)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Could not {action}: {errors}");
        }
    }
}
