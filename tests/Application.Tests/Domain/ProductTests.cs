using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Exceptions;

namespace ProductManagement.Application.Tests.Domain;

public class ProductTests
{
    [Fact]
    public void Create_trims_the_name()
    {
        var product = Product.Create("  Widget  ");

        Assert.Equal("Widget", product.ProductName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_rejects_a_blank_name(string? name)
    {
        Assert.Throws<DomainException>(() => Product.Create(name!));
    }

    [Fact]
    public void Create_accepts_a_name_of_exactly_the_maximum_length()
    {
        var product = Product.Create(new string('a', Product.NameMaxLength));

        Assert.Equal(Product.NameMaxLength, product.ProductName.Length);
    }

    [Fact]
    public void Create_rejects_a_name_longer_than_the_maximum()
    {
        Assert.Throws<DomainException>(() => Product.Create(new string('a', Product.NameMaxLength + 1)));
    }

    [Fact]
    public void Rename_changes_the_name()
    {
        var product = Product.Create("Old");

        product.Rename("New");

        Assert.Equal("New", product.ProductName);
    }

    [Fact]
    public void Rename_rejects_a_blank_name_and_keeps_the_old_one()
    {
        var product = Product.Create("Old");

        Assert.Throws<DomainException>(() => product.Rename(" "));

        Assert.Equal("Old", product.ProductName);
    }

    [Fact]
    public void A_new_product_has_no_items_and_no_audit_values_yet()
    {
        var product = Product.Create("Widget");

        Assert.Empty(product.Items);
        Assert.Null(product.ModifiedBy);
        Assert.Null(product.ModifiedOn);
    }
}
