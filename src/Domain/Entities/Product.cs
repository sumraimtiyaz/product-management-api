using ProductManagement.Domain.Exceptions;

namespace ProductManagement.Domain.Entities;

public class Product : IAuditable
{
    public const int NameMaxLength = 255;
    public const int AuditUserMaxLength = 100;

    private readonly List<Item> _items = [];

    // Required by EF Core; instances are created through Create.
    private Product()
    {
    }

    public int Id { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTime CreatedOn { get; private set; }
    public string? ModifiedBy { get; private set; }
    public DateTime? ModifiedOn { get; private set; }

    public IReadOnlyCollection<Item> Items => _items;

    public static Product Create(string productName) => new() { ProductName = NormalizeName(productName) };

    public void Rename(string productName) => ProductName = NormalizeName(productName);

    private static string NormalizeName(string productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new DomainException("Product name is required.");
        }

        var name = productName.Trim();
        if (name.Length > NameMaxLength)
        {
            throw new DomainException($"Product name cannot exceed {NameMaxLength} characters.");
        }

        return name;
    }
}
