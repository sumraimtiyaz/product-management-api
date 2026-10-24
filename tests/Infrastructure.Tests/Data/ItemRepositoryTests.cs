using ProductManagement.Domain.Entities;
using ProductManagement.Infrastructure.Data.Repositories;
using ProductManagement.Infrastructure.Tests.Support;

namespace ProductManagement.Infrastructure.Tests.Data;

public sealed class ItemRepositoryTests : IDisposable
{
    private readonly TestDatabase _db = new();

    public void Dispose() => _db.Dispose();

    private async Task<(int ProductA, int ProductB, int[] ItemsOfA, int ItemOfB)> SeedAsync()
    {
        await using var context = _db.CreateContext();
        var productA = Product.Create("A");
        var productB = Product.Create("B");
        context.Products.AddRange(productA, productB);
        await context.SaveChangesAsync();

        var itemsOfA = new[] { Item.Create(productA.Id, 1), Item.Create(productA.Id, 2), Item.Create(productA.Id, 3) };
        var itemOfB = Item.Create(productB.Id, 99);
        context.Items.AddRange(itemsOfA);
        context.Items.Add(itemOfB);
        await context.SaveChangesAsync();

        return (productA.Id, productB.Id, itemsOfA.Select(item => item.Id).ToArray(), itemOfB.Id);
    }

    [Fact]
    public async Task GetById_finds_an_item_only_under_its_own_product()
    {
        var seed = await SeedAsync();
        await using var context = _db.CreateContext();
        var repository = new ItemRepository(context);

        var own = await repository.GetByIdAsync(seed.ProductA, seed.ItemsOfA[0], CancellationToken.None);
        var underOtherProduct = await repository.GetByIdAsync(seed.ProductB, seed.ItemsOfA[0], CancellationToken.None);

        Assert.NotNull(own);
        Assert.Null(underOtherProduct);
    }

    [Fact]
    public async Task GetById_is_untracked_and_GetForUpdate_is_tracked()
    {
        var seed = await SeedAsync();

        await using (var readContext = _db.CreateContext())
        {
            await new ItemRepository(readContext).GetByIdAsync(seed.ProductA, seed.ItemsOfA[0], CancellationToken.None);
            Assert.Empty(readContext.ChangeTracker.Entries());
        }

        await using var updateContext = _db.CreateContext();
        await new ItemRepository(updateContext).GetForUpdateAsync(seed.ProductA, seed.ItemsOfA[0], CancellationToken.None);
        Assert.Single(updateContext.ChangeTracker.Entries<Item>());
    }

    [Fact]
    public async Task GetPagedByProduct_returns_only_that_products_items_in_id_order()
    {
        var seed = await SeedAsync();
        await using var context = _db.CreateContext();
        var repository = new ItemRepository(context);

        var firstPage = await repository.GetPagedByProductAsync(seed.ProductA, 1, 2, CancellationToken.None);
        var secondPage = await repository.GetPagedByProductAsync(seed.ProductA, 2, 2, CancellationToken.None);
        var otherProduct = await repository.GetPagedByProductAsync(seed.ProductB, 1, 10, CancellationToken.None);

        Assert.Equal(new[] { 1, 2 }, firstPage.Items.Select(item => item.Quantity).ToArray());
        Assert.Equal(new[] { 3 }, secondPage.Items.Select(item => item.Quantity).ToArray());
        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Equal(new[] { 99 }, otherProduct.Items.Select(item => item.Quantity).ToArray());
    }

    [Fact]
    public async Task An_updated_quantity_is_persisted()
    {
        var seed = await SeedAsync();

        await using (var context = _db.CreateContext())
        {
            var item = await new ItemRepository(context).GetForUpdateAsync(seed.ProductA, seed.ItemsOfA[0], CancellationToken.None);
            item!.UpdateQuantity(42);
            await context.SaveChangesAsync();
        }

        await using var verify = _db.CreateContext();
        var stored = await new ItemRepository(verify).GetByIdAsync(seed.ProductA, seed.ItemsOfA[0], CancellationToken.None);
        Assert.Equal(42, stored!.Quantity);
    }

    [Fact]
    public async Task A_removed_item_is_gone_and_other_items_are_untouched()
    {
        var seed = await SeedAsync();

        await using (var context = _db.CreateContext())
        {
            var repository = new ItemRepository(context);
            var item = await repository.GetForUpdateAsync(seed.ProductA, seed.ItemsOfA[0], CancellationToken.None);
            repository.Remove(item!);
            await context.SaveChangesAsync();
        }

        await using var verify = _db.CreateContext();
        var remaining = await new ItemRepository(verify).GetPagedByProductAsync(seed.ProductA, 1, 10, CancellationToken.None);
        Assert.Equal(2, remaining.TotalCount);
    }
}
