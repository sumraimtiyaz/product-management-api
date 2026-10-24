using ProductManagement.Application.Interfaces;

namespace ProductManagement.Infrastructure.Data;

public sealed class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
