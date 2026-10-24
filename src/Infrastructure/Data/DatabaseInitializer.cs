using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ProductManagement.Infrastructure.Data;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>Apply pending EF Core migrations when the application starts.</summary>
    public bool MigrateOnStartup { get; set; }
}

/// <summary>Prepares the database before the application starts serving requests.</summary>
public interface IDatabaseInitializer
{
    Task InitialiseAsync(CancellationToken cancellationToken = default);
}

public sealed class DatabaseInitializer(
    ApplicationDbContext context,
    IOptions<DatabaseOptions> options,
    ILogger<DatabaseInitializer> logger) : IDatabaseInitializer
{
    public async Task InitialiseAsync(CancellationToken cancellationToken = default)
    {
        if (!options.Value.MigrateOnStartup)
        {
            logger.LogInformation("Automatic migration is disabled; assuming the database schema is already in place.");
            return;
        }

        logger.LogInformation("Applying pending database migrations.");
        await context.Database.MigrateAsync(cancellationToken);
    }
}
