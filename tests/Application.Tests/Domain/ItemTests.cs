using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Exceptions;

namespace ProductManagement.Application.Tests.Domain;

public class ItemTests
{
    [Fact]
    public void Create_sets_product_and_quantity()
    {
        var item = Item.Create(productId: 7, quantity: 3);

        Assert.Equal(7, item.ProductId);
        Assert.Equal(3, item.Quantity);
    }

    [Fact]
    public void Create_accepts_a_quantity_of_zero()
    {
        var item = Item.Create(1, 0);

        Assert.Equal(0, item.Quantity);
    }

    [Fact]
    public void Create_rejects_a_negative_quantity()
    {
        Assert.Throws<DomainException>(() => Item.Create(1, -1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_rejects_a_missing_product(int productId)
    {
        Assert.Throws<DomainException>(() => Item.Create(productId, 1));
    }

    [Fact]
    public void UpdateQuantity_changes_the_quantity()
    {
        var item = Item.Create(1, 1);

        item.UpdateQuantity(10);

        Assert.Equal(10, item.Quantity);
    }

    [Fact]
    public void UpdateQuantity_rejects_a_negative_quantity_and_keeps_the_old_one()
    {
        var item = Item.Create(1, 4);

        Assert.Throws<DomainException>(() => item.UpdateQuantity(-1));

        Assert.Equal(4, item.Quantity);
    }
}
