namespace ProductManagement.Application.DTOs;

/// <summary>A product as returned by the API.</summary>
public sealed record ProductDto(
    int Id,
    string ProductName,
    string CreatedBy,
    DateTime CreatedOn,
    string? ModifiedBy,
    DateTime? ModifiedOn);

/// <summary>Payload for creating a product.</summary>
/// <param name="ProductName">Display name, up to 255 characters.</param>
public sealed record CreateProductRequest(string ProductName);

/// <summary>Payload for replacing a product's data.</summary>
/// <param name="ProductName">Display name, up to 255 characters.</param>
public sealed record UpdateProductRequest(string ProductName);
