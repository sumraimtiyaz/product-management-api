using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ProductManagement.Application.Interfaces;

namespace ProductManagement.Infrastructure.Identity;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public string? UserId => Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);

    // The JWT bearer handler is configured to use the "name" claim as the identity name.
    public string? UserName => Principal?.Identity?.Name;
}
