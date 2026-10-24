using ProductManagement.Application.DTOs;

namespace ProductManagement.Application.Interfaces;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetPagedAsync(PagedQuery query, CancellationToken cancellationToken);

    Task<ProductDto> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken);

    Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken);

    /// <exception cref="Domain.Exceptions.ConflictException">The product still has items.</exception>
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
