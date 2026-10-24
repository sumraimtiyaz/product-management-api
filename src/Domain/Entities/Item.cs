using ProductManagement.Domain.Exceptions;

namespace ProductManagement.Domain.Entities;

public class Item
{
    // Required by EF Core; instances are created through Create.
    private Item()
    {
    }

    public int Id { get; private set; }
    public int ProductId { get; private set; }
    public int Quantity { get; private set; }

    public Product? Product { get; private set; }

    public static Item Create(int productId, int quantity)
    {
        if (productId <= 0)
        {
            throw new DomainException("An item must belong to a product.");
        }

        return new Item { ProductId = productId, Quantity = ValidateQuantity(quantity) };
    }

    public void UpdateQuantity(int quantity) => Quantity = ValidateQuantity(quantity);

    private static int ValidateQuantity(int quantity) =>
        quantity >= 0 ? quantity : throw new DomainException("Quantity cannot be negative.");
}
