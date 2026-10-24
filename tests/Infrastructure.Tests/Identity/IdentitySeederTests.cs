using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ProductManagement.Domain.Enums;
using ProductManagement.Infrastructure.Identity;
using ProductManagement.Infrastructure.Tests.Support;

namespace ProductManagement.Infrastructure.Tests.Identity;

public class IdentitySeederTests
{
    private static Dictionary<string, string?> AdminSettings => new()
    {
        ["AdminSeed:Email"] = "admin@example.com",
        ["AdminSeed:Password"] = "Adm1n!Passw0rd"
    };

    [Fact]
    public async Task Seeding_creates_a_role_for_every_user_role()
    {
        using var host = new IdentityTestHost();
        using var scope = host.CreateScope();

        await scope.ServiceProvider.GetRequiredService<IdentitySeeder>().SeedAsync();

        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        Assert.True(await roles.RoleExistsAsync(RoleNames.User));
        Assert.True(await roles.RoleExistsAsync(RoleNames.Admin));
    }

    [Fact]
    public async Task Seeding_creates_the_configured_administrator_once()
    {
        using var host = new IdentityTestHost(AdminSettings);
        using var scope = host.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await users.FindByEmailAsync("admin@example.com");
        Assert.NotNull(admin);
        Assert.True(await users.IsInRoleAsync(admin, RoleNames.Admin));
        Assert.Single(users.Users.ToList());
    }

    [Fact]
    public async Task Seeding_skips_the_administrator_when_none_is_configured()
    {
        using var host = new IdentityTestHost();
        using var scope = host.CreateScope();

        await scope.ServiceProvider.GetRequiredService<IdentitySeeder>().SeedAsync();

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.Empty(users.Users.ToList());
    }

    [Fact]
    public async Task Seeding_fails_loudly_when_the_administrator_password_breaks_the_policy()
    {
        using var host = new IdentityTestHost(new Dictionary<string, string?>
        {
            ["AdminSeed:Email"] = "admin@example.com",
            ["AdminSeed:Password"] = "weak"
        });
        using var scope = host.CreateScope();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => scope.ServiceProvider.GetRequiredService<IdentitySeeder>().SeedAsync());
    }
}
