using ProductManagement.Application.DTOs;
using ProductManagement.Application.Interfaces;
using ProductManagement.Application.Mapping;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Exceptions;

namespace ProductManagement.Application.Services;

public sealed class ItemService(IProductRepository products, IItemRepository items, IUnitOfWork unitOfWork)
    : IItemService
{
    public async Task<PagedResult<ItemDto>> GetPagedAsync(
        int productId, PagedQuery query, CancellationToken cancellationToken)
    {
        await EnsureProductExistsAsync(productId, cancellationToken);

        var page = await items.GetPagedByProductAsync(productId, query.PageNumber, query.PageSize, cancellationToken);
        return page.Map(item => item.ToDto());
    }

    public async Task<ItemDto> GetByIdAsync(int productId, int itemId, CancellationToken cancellationToken)
    {
        var item = await items.GetByIdAsync(productId, itemId, cancellationToken)
            ?? throw new NotFoundException(nameof(Item), itemId);

        return item.ToDto();
    }

    public async Task<ItemDto> CreateAsync(
        int productId, CreateItemRequest request, CancellationToken cancellationToken)
    {
        await EnsureProductExistsAsync(productId, cancellationToken);

        var item = Item.Create(productId, request.Quantity);

        items.Add(item);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return item.ToDto();
    }

    public async Task<ItemDto> UpdateAsync(
        int productId, int itemId, UpdateItemRequest request, CancellationToken cancellationToken)
    {
        var item = await items.GetForUpdateAsync(productId, itemId, cancellationToken)
            ?? throw new NotFoundException(nameof(Item), itemId);

        item.UpdateQuantity(request.Quantity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return item.ToDto();
    }

    public async Task DeleteAsync(int productId, int itemId, CancellationToken cancellationToken)
    {
        var item = await items.GetForUpdateAsync(productId, itemId, cancellationToken)
            ?? throw new NotFoundException(nameof(Item), itemId);

        items.Remove(item);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureProductExistsAsync(int productId, CancellationToken cancellationToken)
    {
        if (!await products.ExistsAsync(productId, cancellationToken))
        {
            throw new NotFoundException(nameof(Product), productId);
        }
    }
}
