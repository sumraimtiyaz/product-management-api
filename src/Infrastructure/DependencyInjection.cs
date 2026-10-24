using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProductManagement.Application.Interfaces;
using ProductManagement.Application.Validators;
using ProductManagement.Infrastructure.Data;
using ProductManagement.Infrastructure.Data.Repositories;
using ProductManagement.Infrastructure.Identity;

namespace ProductManagement.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "DefaultConnection";

    /// <summary>Registers persistence, identity and token services.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<DatabaseOptions>().BindConfiguration(DatabaseOptions.SectionName);
        services.AddOptions<AdminSeedOptions>().BindConfiguration(AdminSeedOptions.SectionName);

        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<AuditSaveChangesInterceptor>();

        // The connection string is read when the context is first created, not while services are registered.
        services.AddDbContext<ApplicationDbContext>((provider, options) =>
        {
            var connectionString = provider.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
            options.AddInterceptors(provider.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        services.AddIdentityCore<ApplicationUser>(identity =>
            {
                identity.User.RequireUniqueEmail = true;
                identity.Password.RequiredLength = RegisterRequestValidator.PasswordMinLength;
                identity.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
        services.AddScoped<IdentitySeeder>();

        return services;
    }

    /// <summary>Prepares the database and seeds roles (and the first administrator, when configured).</summary>
    public static async Task InitialiseInfrastructureAsync(
        this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>().InitialiseAsync(cancellationToken);
        await scope.ServiceProvider.GetRequiredService<IdentitySeeder>().SeedAsync(cancellationToken);
    }
}
