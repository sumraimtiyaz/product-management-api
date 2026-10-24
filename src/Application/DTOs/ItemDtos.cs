namespace ProductManagement.Application.DTOs;

/// <summary>An item (a quantity of a product) as returned by the API.</summary>
public sealed record ItemDto(int Id, int ProductId, int Quantity);

/// <summary>Payload for creating an item under a product.</summary>
/// <param name="Quantity">Quantity, zero or greater.</param>
public sealed record CreateItemRequest(int Quantity);

/// <summary>Payload for replacing an item's data.</summary>
/// <param name="Quantity">Quantity, zero or greater.</param>
public sealed record UpdateItemRequest(int Quantity);
