using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ProductManagement.Application.Interfaces;
using ProductManagement.Domain.Entities;

namespace ProductManagement.Infrastructure.Data;

/// <summary>
/// Stamps CreatedBy/CreatedOn and ModifiedBy/ModifiedOn on every <see cref="IAuditable"/> entity in one place,
/// so no service or repository has to remember to.
/// </summary>
public sealed class AuditSaveChangesInterceptor(ICurrentUserService currentUser, TimeProvider clock)
    : SaveChangesInterceptor
{
    /// <summary>Recorded when there is no signed-in user, for example during background work.</summary>
    public const string SystemUser = "system";

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = TruncateToSeconds(clock.GetUtcNow().UtcDateTime);
        var user = currentUser.UserName ?? SystemUser;

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(nameof(IAuditable.CreatedBy)).CurrentValue = user;
                    entry.Property(nameof(IAuditable.CreatedOn)).CurrentValue = now;
                    break;

                case EntityState.Modified:
                    entry.Property(nameof(IAuditable.CreatedBy)).IsModified = false;
                    entry.Property(nameof(IAuditable.CreatedOn)).IsModified = false;
                    entry.Property(nameof(IAuditable.ModifiedBy)).CurrentValue = user;
                    entry.Property(nameof(IAuditable.ModifiedOn)).CurrentValue = now;
                    break;
            }
        }
    }

    // The SQL Server datetime columns round to about 3 ms. Whole seconds are stored exactly, so the value
    // returned right after a create is identical to what later reads return.
    private static DateTime TruncateToSeconds(DateTime value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
}
