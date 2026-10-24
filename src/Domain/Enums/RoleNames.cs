namespace ProductManagement.Domain.Enums;

/// <summary>
/// Compile-time role names, derived from <see cref="UserRole"/> so they can be used in attributes
/// such as <c>[Authorize(Roles = RoleNames.Admin)]</c> without magic strings.
/// </summary>
public static class RoleNames
{
    public const string User = nameof(UserRole.User);
    public const string Admin = nameof(UserRole.Admin);
}
