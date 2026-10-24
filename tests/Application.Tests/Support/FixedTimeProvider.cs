namespace ProductManagement.Application.Tests.Support;

/// <summary>A clock that always reports the same instant, so time-dependent behaviour is deterministic.</summary>
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
