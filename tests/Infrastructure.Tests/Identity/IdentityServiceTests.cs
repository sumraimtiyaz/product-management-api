using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ProductManagement.Application.Interfaces;
using ProductManagement.Domain.Enums;
using ProductManagement.Domain.Exceptions;
using ProductManagement.Infrastructure.Identity;
using ProductManagement.Infrastructure.Tests.Support;

namespace ProductManagement.Infrastructure.Tests.Identity;

public sealed class IdentityServiceTests : IDisposable
{
    private const string Email = "alice@example.com";
    private const string Password = "Str0ng!Passw0rd";

    private readonly IdentityTestHost _host = new();

    public void Dispose() => _host.Dispose();

    private async Task<IServiceScope> CreateScopeWithRolesAsync()
    {
        var scope = _host.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IdentitySeeder>().SeedAsync();
        return scope;
    }

    [Fact]
    public async Task Create_registers_the_user_in_the_requested_role()
    {
        using var scope = await CreateScopeWithRolesAsync();
        var identity = scope.ServiceProvider.GetRequiredService<IIdentityService>();

        var created = await identity.CreateUserAsync(Email, Password, RoleNames.User, CancellationToken.None);
        var found = await identity.FindByIdAsync(created.Id, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(Email, found.Email);
        Assert.Equal(new[] { RoleNames.User }, found.Roles.ToArray());
    }

    [Fact]
    public async Task Create_rejects_an_email_that_is_already_registered_whatever_its_casing()
    {
        using var scope = await CreateScopeWithRolesAsync();
        var identity = scope.ServiceProvider.GetRequiredService<IIdentityService>();
        await identity.CreateUserAsync(Email, Password, RoleNames.User, CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(
            () => identity.CreateUserAsync(Email.ToUpperInvariant(), Password, RoleNames.User, CancellationToken.None));
    }

    [Fact]
    public async Task Create_rejects_a_password_that_breaks_the_password_policy()
    {
        using var scope = await CreateScopeWithRolesAsync();
        var identity = scope.ServiceProvider.GetRequiredService<IIdentityService>();

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => identity.CreateUserAsync(Email, "alllowercase", RoleNames.User, CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.PropertyName == "PasswordRequiresUpper");
    }

    [Fact]
    public async Task Valid_credentials_return_the_user_and_wrong_ones_return_nothing()
    {
        using var scope = await CreateScopeWithRolesAsync();
        var identity = scope.ServiceProvider.GetRequiredService<IIdentityService>();
        await identity.CreateUserAsync(Email, Password, RoleNames.User, CancellationToken.None);

        var valid = await identity.ValidateCredentialsAsync(Email, Password, CancellationToken.None);
        var wrongPassword = await identity.ValidateCredentialsAsync(Email, "Wr0ng!Passw0rd", CancellationToken.None);
        var unknownUser = await identity.ValidateCredentialsAsync("nobody@example.com", Password, CancellationToken.None);

        Assert.NotNull(valid);
        Assert.Equal(Email, valid.Email);
        Assert.Null(wrongPassword);
        Assert.Null(unknownUser);
    }

    [Fact]
    public async Task Repeated_failures_lock_the_account_even_for_the_correct_password()
    {
        using var scope = await CreateScopeWithRolesAsync();
        var identity = scope.ServiceProvider.GetRequiredService<IIdentityService>();
        await identity.CreateUserAsync(Email, Password, RoleNames.User, CancellationToken.None);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await identity.ValidateCredentialsAsync(Email, "Wr0ng!Passw0rd", CancellationToken.None);
        }

        Assert.Null(await identity.ValidateCredentialsAsync(Email, Password, CancellationToken.None));
    }

    [Fact]
    public async Task FindById_returns_nothing_for_an_unknown_user()
    {
        using var scope = await CreateScopeWithRolesAsync();
        var identity = scope.ServiceProvider.GetRequiredService<IIdentityService>();

        Assert.Null(await identity.FindByIdAsync("missing", CancellationToken.None));
    }
}
