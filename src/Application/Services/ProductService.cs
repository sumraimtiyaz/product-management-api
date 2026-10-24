using ProductManagement.Application.DTOs;
using ProductManagement.Application.Interfaces;
using ProductManagement.Application.Mapping;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Exceptions;

namespace ProductManagement.Application.Services;

public sealed class ProductService(IProductRepository products, IUnitOfWork unitOfWork) : IProductService
{
    public async Task<PagedResult<ProductDto>> GetPagedAsync(PagedQuery query, CancellationToken cancellationToken)
    {
        var page = await products.GetPagedAsync(query.PageNumber, query.PageSize, cancellationToken);
        return page.Map(product => product.ToDto());
    }

    public async Task<ProductDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), id);

        return product.ToDto();
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var product = Product.Create(request.ProductName);

        products.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return product.ToDto();
    }

    public async Task<ProductDto> UpdateAsync(
        int id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await products.GetForUpdateAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), id);

        product.Rename(request.ProductName);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return product.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var product = await products.GetForUpdateAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), id);

        if (await products.HasItemsAsync(id, cancellationToken))
        {
            throw new ConflictException("The product still has items. Delete its items first.");
        }

        products.Remove(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
