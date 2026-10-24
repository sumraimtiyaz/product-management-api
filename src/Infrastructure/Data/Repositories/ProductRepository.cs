using Microsoft.EntityFrameworkCore;
using ProductManagement.Application.DTOs;
using ProductManagement.Application.Interfaces;
using ProductManagement.Domain.Entities;

namespace ProductManagement.Infrastructure.Data.Repositories;

public sealed class ProductRepository(ApplicationDbContext context) : IProductRepository
{
    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        context.Products.AsNoTracking().FirstOrDefaultAsync(product => product.Id == id, cancellationToken);

    public Task<Product?> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
        context.Products.FirstOrDefaultAsync(product => product.Id == id, cancellationToken);

    public Task<PagedResult<Product>> GetPagedAsync(
        int pageNumber, int pageSize, CancellationToken cancellationToken) =>
        context.Products
            .AsNoTracking()
            .OrderBy(product => product.Id)
            .ToPagedResultAsync(pageNumber, pageSize, cancellationToken);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken) =>
        context.Products.AnyAsync(product => product.Id == id, cancellationToken);

    public Task<bool> HasItemsAsync(int id, CancellationToken cancellationToken) =>
        context.Items.AnyAsync(item => item.ProductId == id, cancellationToken);

    public void Add(Product product) => context.Products.Add(product);

    public void Remove(Product product) => context.Products.Remove(product);
}
