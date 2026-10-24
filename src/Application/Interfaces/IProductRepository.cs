using ProductManagement.Application.DTOs;
using ProductManagement.Domain.Entities;

namespace ProductManagement.Application.Interfaces;

public interface IProductRepository
{
    /// <summary>Gets a product for reading. The returned entity is not tracked.</summary>
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Gets a product that is about to be modified. The returned entity is tracked.</summary>
    Task<Product?> GetForUpdateAsync(int id, CancellationToken cancellationToken);

    Task<PagedResult<Product>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);

    Task<bool> HasItemsAsync(int id, CancellationToken cancellationToken);

    void Add(Product product);

    void Remove(Product product);
}
