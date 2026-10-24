using ProductManagement.Application.DTOs;

namespace ProductManagement.Application.Interfaces;

public interface IItemService
{
    Task<PagedResult<ItemDto>> GetPagedAsync(int productId, PagedQuery query, CancellationToken cancellationToken);

    Task<ItemDto> GetByIdAsync(int productId, int itemId, CancellationToken cancellationToken);

    Task<ItemDto> CreateAsync(int productId, CreateItemRequest request, CancellationToken cancellationToken);

    Task<ItemDto> UpdateAsync(
        int productId, int itemId, UpdateItemRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(int productId, int itemId, CancellationToken cancellationToken);
}
