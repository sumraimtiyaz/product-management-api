namespace ProductManagement.Infrastructure.Tests.Support;

/// <summary>A clock tests can set and move forward.</summary>
public sealed class TestClock(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}
