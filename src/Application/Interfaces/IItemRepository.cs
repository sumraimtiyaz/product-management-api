using ProductManagement.Application.DTOs;
using ProductManagement.Domain.Entities;

namespace ProductManagement.Application.Interfaces;

public interface IItemRepository
{
    /// <summary>Gets an item of the given product for reading. The returned entity is not tracked.</summary>
    Task<Item?> GetByIdAsync(int productId, int itemId, CancellationToken cancellationToken);

    /// <summary>Gets an item of the given product that is about to be modified. The returned entity is tracked.</summary>
    Task<Item?> GetForUpdateAsync(int productId, int itemId, CancellationToken cancellationToken);

    Task<PagedResult<Item>> GetPagedByProductAsync(
        int productId, int pageNumber, int pageSize, CancellationToken cancellationToken);

    void Add(Item item);

    void Remove(Item item);
}
