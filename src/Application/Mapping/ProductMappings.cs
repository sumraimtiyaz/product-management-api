using ProductManagement.Application.DTOs;
using ProductManagement.Domain.Entities;

namespace ProductManagement.Application.Mapping;

public static class ProductMappings
{
    public static ProductDto ToDto(this Product product) => new(
        product.Id,
        product.ProductName,
        product.CreatedBy,
        product.CreatedOn.AsUtc(),
        product.ModifiedBy,
        product.ModifiedOn?.AsUtc());

    public static ItemDto ToDto(this Item item) => new(item.Id, item.ProductId, item.Quantity);

    /// <summary>
    /// Audit timestamps are stored as UTC in <c>datetime</c> columns, which lose their kind on read;
    /// marking them as UTC makes them serialize with a trailing <c>Z</c>.
    /// </summary>
    private static DateTime AsUtc(this DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
