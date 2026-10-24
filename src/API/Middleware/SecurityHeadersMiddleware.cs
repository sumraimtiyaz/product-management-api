using Microsoft.Net.Http.Headers;

namespace ProductManagement.Api.Middleware;

/// <summary>Adds defensive response headers. Swagger UI needs inline scripts, so it is exempt from the strict CSP.</summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    private const string StrictContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers[HeaderNames.XContentTypeOptions] = "nosniff";
        headers[HeaderNames.XFrameOptions] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

        if (!context.Request.Path.StartsWithSegments("/swagger"))
        {
            headers[HeaderNames.ContentSecurityPolicy] = StrictContentSecurityPolicy;
        }

        return next(context);
    }
}
