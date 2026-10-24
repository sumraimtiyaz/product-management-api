using Microsoft.EntityFrameworkCore;
using ProductManagement.Application.DTOs;
using ProductManagement.Application.Interfaces;
using ProductManagement.Domain.Entities;

namespace ProductManagement.Infrastructure.Data.Repositories;

public sealed class ItemRepository(ApplicationDbContext context) : IItemRepository
{
    public Task<Item?> GetByIdAsync(int productId, int itemId, CancellationToken cancellationToken) =>
        context.Items
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == itemId && item.ProductId == productId, cancellationToken);

    public Task<Item?> GetForUpdateAsync(int productId, int itemId, CancellationToken cancellationToken) =>
        context.Items
            .FirstOrDefaultAsync(item => item.Id == itemId && item.ProductId == productId, cancellationToken);

    public Task<PagedResult<Item>> GetPagedByProductAsync(
        int productId, int pageNumber, int pageSize, CancellationToken cancellationToken) =>
        context.Items
            .AsNoTracking()
            .Where(item => item.ProductId == productId)
            .OrderBy(item => item.Id)
            .ToPagedResultAsync(pageNumber, pageSize, cancellationToken);

    public void Add(Item item) => context.Items.Add(item);

    public void Remove(Item item) => context.Items.Remove(item);
}
