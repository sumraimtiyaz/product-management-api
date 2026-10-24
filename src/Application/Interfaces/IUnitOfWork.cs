namespace ProductManagement.Application.Interfaces;

/// <summary>Commits all changes made through the repositories as one unit.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
