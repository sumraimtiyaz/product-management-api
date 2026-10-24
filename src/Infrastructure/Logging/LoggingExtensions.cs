using Microsoft.Extensions.Hosting;
using Serilog;

namespace ProductManagement.Infrastructure.Logging;

public static class LoggingExtensions
{
    /// <summary>
    /// Replaces the default logging with Serilog, configured from the "Serilog" configuration section
    /// (JSON events on the console by default).
    /// </summary>
    public static IHostBuilder AddStructuredLogging(this IHostBuilder host) =>
        host.UseSerilog((context, services, logger) => logger
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());
}
