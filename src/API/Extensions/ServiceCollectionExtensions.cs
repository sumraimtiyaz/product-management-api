using Asp.Versioning;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;
using ProductManagement.Api.Filters;

namespace ProductManagement.Api.Extensions;

public sealed class CorsSettings
{
    public const string SectionName = "Cors";

    /// <summary>Origins allowed to call the API from a browser. Empty means no cross-origin access.</summary>
    public string[] AllowedOrigins { get; set; } = [];
}

public static class ServiceCollectionExtensions
{
    public const string CorsPolicyName = "DefaultCors";

    /// <summary>Registers everything the HTTP layer needs: MVC, versioning, security, CORS, compression and docs.</summary>
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers(options => options.Filters.Add<ValidationFilter>());
        services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

        // Gives 401/403/404 responses that have no body the same problem+json shape as other errors.
        services.AddProblemDetails();
        services.AddResponseCompression(options => options.EnableForHttps = true);

        services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        services.AddJwtAuthentication();
        services.AddSecureByDefaultAuthorization();
        services.AddConfiguredCors();
        services.AddSwaggerDocs();

        return services;
    }

    private static IServiceCollection AddConfiguredCors(this IServiceCollection services)
    {
        services.AddOptions<CorsSettings>().BindConfiguration(CorsSettings.SectionName);
        services.AddCors();

        // Built from options so the allowed origins are read once configuration is fully loaded.
        services.AddOptions<CorsOptions>().Configure<IOptions<CorsSettings>>((cors, settings) =>
            cors.AddPolicy(CorsPolicyName, policy =>
            {
                var origins = settings.Value.AllowedOrigins;
                if (origins.Length > 0)
                {
                    policy.WithOrigins(origins)
                        .WithMethods(HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete)
                        .AllowAnyHeader();
                }
            }));

        return services;
    }
}
