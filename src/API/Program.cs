using System.Security.Authentication;
using ProductManagement.Api.Extensions;
using ProductManagement.Application;
using ProductManagement.Infrastructure;
using ProductManagement.Infrastructure.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddStructuredLogging();

builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.AddServerHeader = false;
    kestrel.ConfigureHttpsDefaults(https => https.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13);
});

builder.Services
    .AddApplication()
    .AddInfrastructure()
    .AddApiServices();

var app = builder.Build();

await app.Services.InitialiseInfrastructureAsync();

app.UseApiPipeline();
app.MapControllers();

await app.RunAsync();

// Makes the entry point visible to WebApplicationFactory in the integration tests.
public partial class Program
{
}
