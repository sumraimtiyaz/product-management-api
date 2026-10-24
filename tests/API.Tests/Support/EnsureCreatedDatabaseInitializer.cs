using ProductManagement.Infrastructure.Data;

namespace ProductManagement.Api.Tests.Support;

/// <summary>
/// Creates the schema straight from the EF model. The checked-in migrations target SQL Server,
/// so the SQLite database used by the tests cannot apply them.
/// </summary>
public sealed class EnsureCreatedDatabaseInitializer(ApplicationDbContext context) : IDatabaseInitializer
{
    public async Task InitialiseAsync(CancellationToken cancellationToken = default) =>
        await context.Database.EnsureCreatedAsync(cancellationToken);
}
