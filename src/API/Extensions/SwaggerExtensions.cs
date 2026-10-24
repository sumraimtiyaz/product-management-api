using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ProductManagement.Api.Extensions;

public static class SwaggerExtensions
{
    private const string BearerScheme = "Bearer";

    public static IServiceCollection AddSwaggerDocs(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            // The API and Application projects both document their types with XML comments.
            foreach (var xmlFile in Directory.EnumerateFiles(AppContext.BaseDirectory, "ProductManagement.*.xml"))
            {
                options.IncludeXmlComments(xmlFile, includeControllerXmlComments: true);
            }

            options.AddSecurityDefinition(BearerScheme, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste the access token returned by /auth/login. The 'Bearer ' prefix is added for you."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = BearerScheme }
                }] = Array.Empty<string>()
            });
        });

        services.ConfigureOptions<ConfigureSwaggerOptions>();

        return services;
    }

    public static WebApplication UseSwaggerDocs(this WebApplication app)
    {
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
            }
        });

        return app;
    }

    /// <summary>Creates one Swagger document per API version.</summary>
    private sealed class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
        : IConfigureOptions<SwaggerGenOptions>
    {
        public void Configure(SwaggerGenOptions options)
        {
            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(description.GroupName, new OpenApiInfo
                {
                    Title = "Product Management API",
                    Version = description.ApiVersion.ToString(),
                    Description = description.IsDeprecated
                        ? "This version of the API has been deprecated."
                        : "RESTful API for managing products and their items."
                });
            }
        }
    }
}
