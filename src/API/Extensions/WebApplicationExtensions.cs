using ProductManagement.Api.Middleware;
using Serilog;

namespace ProductManagement.Api.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>Builds the request pipeline. The order of the middleware below matters.</summary>
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseMiddleware<SecurityHeadersMiddleware>();

        // Request logging wraps the exception handler, so it records the status code the client actually receives.
        app.UseSerilogRequestLogging();
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseResponseCompression();
        app.UseStatusCodePages();

        if (app.Configuration.GetValue("Swagger:Enabled", defaultValue: false))
        {
            app.UseSwaggerDocs();
        }

        app.UseCors(ServiceCollectionExtensions.CorsPolicyName);
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
