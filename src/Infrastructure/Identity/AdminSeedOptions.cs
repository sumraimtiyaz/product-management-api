namespace ProductManagement.Infrastructure.Identity;

/// <summary>Optional first administrator account created at startup when both values are supplied.</summary>
public sealed class AdminSeedOptions
{
    public const string SectionName = "AdminSeed";

    public string? Email { get; set; }

    public string? Password { get; set; }
}
