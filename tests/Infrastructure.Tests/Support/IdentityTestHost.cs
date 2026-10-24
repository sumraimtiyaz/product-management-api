using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProductManagement.Infrastructure.Data;

namespace ProductManagement.Infrastructure.Tests.Support;

/// <summary>
/// The real infrastructure registrations (identity, repositories, token service) running on SQLite,
/// so identity behaviour is tested against the actual ASP.NET Core Identity stores.
/// </summary>
public sealed class IdentityTestHost : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public IdentityTestHost(Dictionary<string, string?>? settings = null)
    {
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(
            new ConfigurationBuilder().AddInMemoryCollection(settings ?? new Dictionary<string, string?>()).Build());
        services.AddInfrastructure();

        services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
        services.AddDbContext<ApplicationDbContext>((provider, options) => options
            .UseSqlite(_connection)
            .AddInterceptors(provider.GetRequiredService<AuditSaveChangesInterceptor>()));

        Services = services.BuildServiceProvider(validateScopes: true);

        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();
    }

    public ServiceProvider Services { get; }

    public IServiceScope CreateScope() => Services.CreateScope();

    public void Dispose()
    {
        Services.Dispose();
        _connection.Dispose();
    }
}
