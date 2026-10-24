namespace ProductManagement.Application.Interfaces;

/// <summary>The user making the current request, if any.</summary>
public interface ICurrentUserService
{
    string? UserId { get; }

    /// <summary>The user name recorded in audit columns such as <c>CreatedBy</c>.</summary>
    string? UserName { get; }
}
