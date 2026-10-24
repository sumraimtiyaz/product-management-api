using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProductManagement.Application.DTOs;
using ProductManagement.Infrastructure.Data;

namespace ProductManagement.Api.Tests.Support;

/// <summary>
/// Hosts the real application in memory. Only two things differ from production: the database is an
/// in-memory SQLite database, and the settings that normally come from secrets are supplied here.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "Adm1n!Passw0rd";
    public const string UserPassword = "Str0ng!Passw0rd";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public ApiFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "integration-tests-only-signing-key-0123456789",
                ["ConnectionStrings:DefaultConnection"] = "unused-in-tests",
                ["AdminSeed:Email"] = AdminEmail,
                ["AdminSeed:Password"] = AdminPassword,
                ["Serilog:MinimumLevel:Default"] = "Warning"
            }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>((provider, options) => options
                .UseSqlite(_connection)
                .AddInterceptors(provider.GetRequiredService<AuditSaveChangesInterceptor>()));

            services.RemoveAll<IDatabaseInitializer>();
            services.AddScoped<IDatabaseInitializer, EnsureCreatedDatabaseInitializer>();
        });
    }

    /// <summary>Registers a brand-new user (role <c>User</c>) and returns a client that is signed in as them.</summary>
    public async Task<TestUser> CreateUserAsync()
    {
        var email = $"user-{Guid.NewGuid():N}@test.local";
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, UserPassword));
        response.EnsureSuccessStatusCode();

        return SignIn(client, email, await response.ReadAsync<AuthResponse>());
    }

    /// <summary>Signs in as the seeded administrator.</summary>
    public async Task<TestUser> CreateAdminAsync()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(AdminEmail, AdminPassword));
        response.EnsureSuccessStatusCode();

        return SignIn(client, AdminEmail, await response.ReadAsync<AuthResponse>());
    }

    private static TestUser SignIn(HttpClient client, string email, AuthResponse tokens)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return new TestUser(client, email, tokens);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }
}

/// <summary>A signed-in test user: their client, email and the tokens they were issued.</summary>
public sealed record TestUser(HttpClient Client, string Email, AuthResponse Tokens);
