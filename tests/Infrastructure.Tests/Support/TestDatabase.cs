using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProductManagement.Infrastructure.Data;

namespace ProductManagement.Infrastructure.Tests.Support;

/// <summary>
/// A throw-away SQLite database holding the real EF Core model, with the audit interceptor attached.
/// Contexts created from it share one connection, so data written by one is visible to the next.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public TestDatabase()
    {
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public FakeCurrentUser User { get; } = new() { UserName = "alice" };

    public TestClock Clock { get; } = new(new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero));

    public ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new AuditSaveChangesInterceptor(User, Clock))
            .Options;

        return new ApplicationDbContext(options);
    }

    public void Dispose() => _connection.Dispose();
}
