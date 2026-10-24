using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProductManagement.Infrastructure.Data;

/// <summary>Lets the EF Core tools (for example <c>dotnet ef migrations add</c>) create the context without running the app.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private const string FallbackConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=ProductManagement;Trusted_Connection=True;TrustServerCertificate=True";

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? FallbackConnectionString;

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }
}
