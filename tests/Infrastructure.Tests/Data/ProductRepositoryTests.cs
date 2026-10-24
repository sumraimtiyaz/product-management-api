using Microsoft.EntityFrameworkCore;
using ProductManagement.Domain.Entities;
using ProductManagement.Infrastructure.Data;
using ProductManagement.Infrastructure.Data.Repositories;
using ProductManagement.Infrastructure.Tests.Support;

namespace ProductManagement.Infrastructure.Tests.Data;

public sealed class ProductRepositoryTests : IDisposable
{
    private readonly TestDatabase _db = new();

    public void Dispose() => _db.Dispose();

    private async Task<int[]> SeedProductsAsync(params string[] names)
    {
        await using var context = _db.CreateContext();
        var products = names.Select(Product.Create).ToArray();
        context.Products.AddRange(products);
        await context.SaveChangesAsync();
        return products.Select(product => product.Id).ToArray();
    }

    [Fact]
    public async Task An_added_product_can_be_read_back_with_its_audit_values()
    {
        await using (var writeContext = _db.CreateContext())
        {
            var repository = new ProductRepository(writeContext);
            repository.Add(Product.Create("Widget"));
            await new UnitOfWork(writeContext).SaveChangesAsync(CancellationToken.None);
        }

        await using var readContext = _db.CreateContext();
        var stored = await new ProductRepository(readContext).GetPagedAsync(1, 10, CancellationToken.None);

        var product = Assert.Single(stored.Items);
        Assert.Equal("Widget", product.ProductName);
        Assert.Equal("alice", product.CreatedBy);
        Assert.Equal(_db.Clock.Now.UtcDateTime, product.CreatedOn);
    }

    [Fact]
    public async Task GetById_returns_an_untracked_entity()
    {
        var id = (await SeedProductsAsync("Widget"))[0];

        await using var context = _db.CreateContext();
        var product = await new ProductRepository(context).GetByIdAsync(id, CancellationToken.None);

        Assert.NotNull(product);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetForUpdate_returns_a_tracked_entity()
    {
        var id = (await SeedProductsAsync("Widget"))[0];

        await using var context = _db.CreateContext();
        var product = await new ProductRepository(context).GetForUpdateAsync(id, CancellationToken.None);

        Assert.NotNull(product);
        Assert.Single(context.ChangeTracker.Entries<Product>());
    }

    [Fact]
    public async Task GetById_returns_null_for_an_unknown_product()
    {
        await using var context = _db.CreateContext();

        Assert.Null(await new ProductRepository(context).GetByIdAsync(404, CancellationToken.None));
    }

    [Fact]
    public async Task GetPaged_returns_the_requested_page_in_id_order_with_the_total()
    {
        await SeedProductsAsync("P1", "P2", "P3", "P4", "P5");
        await using var context = _db.CreateContext();
        var repository = new ProductRepository(context);

        var second = await repository.GetPagedAsync(2, 2, CancellationToken.None);
        var last = await repository.GetPagedAsync(3, 2, CancellationToken.None);

        Assert.Equal(new[] { "P3", "P4" }, second.Items.Select(product => product.ProductName).ToArray());
        Assert.Equal(5, second.TotalCount);
        Assert.Equal(3, second.TotalPages);
        Assert.Equal(new[] { "P5" }, last.Items.Select(product => product.ProductName).ToArray());
    }

    [Theory]
    [InlineData(10, 20)]
    [InlineData(int.MaxValue, 100)]
    public async Task GetPaged_beyond_the_last_page_is_empty_but_still_reports_the_total(int pageNumber, int pageSize)
    {
        await SeedProductsAsync("P1", "P2");
        await using var context = _db.CreateContext();

        var page = await new ProductRepository(context).GetPagedAsync(pageNumber, pageSize, CancellationToken.None);

        Assert.Empty(page.Items);
        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task GetPaged_does_not_track_the_products_it_returns()
    {
        await SeedProductsAsync("P1");
        await using var context = _db.CreateContext();

        await new ProductRepository(context).GetPagedAsync(1, 10, CancellationToken.None);

        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Exists_and_HasItems_reflect_the_stored_data()
    {
        var ids = await SeedProductsAsync("With items", "Without items");
        await using (var seed = _db.CreateContext())
        {
            seed.Items.Add(Item.Create(ids[0], 2));
            await seed.SaveChangesAsync();
        }

        await using var context = _db.CreateContext();
        var repository = new ProductRepository(context);

        Assert.True(await repository.ExistsAsync(ids[0], CancellationToken.None));
        Assert.False(await repository.ExistsAsync(404, CancellationToken.None));
        Assert.True(await repository.HasItemsAsync(ids[0], CancellationToken.None));
        Assert.False(await repository.HasItemsAsync(ids[1], CancellationToken.None));
    }

    [Fact]
    public async Task Remove_deletes_a_product_without_items()
    {
        var id = (await SeedProductsAsync("Widget"))[0];

        await using (var context = _db.CreateContext())
        {
            var repository = new ProductRepository(context);
            var product = await repository.GetForUpdateAsync(id, CancellationToken.None);
            repository.Remove(product!);
            await context.SaveChangesAsync();
        }

        await using var verify = _db.CreateContext();
        Assert.False(await new ProductRepository(verify).ExistsAsync(id, CancellationToken.None));
    }

    [Fact]
    public async Task The_database_refuses_to_delete_a_product_that_still_has_items()
    {
        var id = (await SeedProductsAsync("Widget"))[0];
        await using (var seed = _db.CreateContext())
        {
            seed.Items.Add(Item.Create(id, 1));
            await seed.SaveChangesAsync();
        }

        await using var context = _db.CreateContext();
        var product = await new ProductRepository(context).GetForUpdateAsync(id, CancellationToken.None);
        context.Products.Remove(product!);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
