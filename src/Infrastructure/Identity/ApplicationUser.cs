using Microsoft.AspNetCore.Identity;

namespace ProductManagement.Infrastructure.Identity;

/// <summary>The ASP.NET Core Identity user. Kept in Infrastructure so the domain stays free of framework types.</summary>
public class ApplicationUser : IdentityUser;
