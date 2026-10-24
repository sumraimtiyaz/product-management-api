using ProductManagement.Application.Interfaces;

namespace ProductManagement.Infrastructure.Tests.Support;

public sealed class FakeCurrentUser : ICurrentUserService
{
    public string? UserId { get; set; }

    public string? UserName { get; set; }
}
