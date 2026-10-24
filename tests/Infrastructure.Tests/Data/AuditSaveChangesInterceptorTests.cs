using Microsoft.EntityFrameworkCore;
using ProductManagement.Domain.Entities;
using ProductManagement.Infrastructure.Data;
using ProductManagement.Infrastructure.Tests.Support;

namespace ProductManagement.Infrastructure.Tests.Data;

public sealed class AuditSaveChangesInterceptorTests : IDisposable
{
    private readonly TestDatabase _db = new();

    public void Dispose() => _db.Dispose();

    private async Task<int> AddProductAsync(string name)
    {
        await using var context = _db.CreateContext();
        var product = Product.Create(name);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        return product.Id;
    }

    private async Task<Product> ReadProductAsync(int id)
    {
        await using var context = _db.CreateContext();
        return await context.Products.AsNoTracking().SingleAsync(product => product.Id == id);
    }

    [Fact]
    public async Task Adding_a_product_stamps_who_created_it_and_when()
    {
        var created = _db.Clock.Now;

        var id = await AddProductAsync("Widget");
        var stored = await ReadProductAsync(id);

        Assert.Equal("alice", stored.CreatedBy);
        Assert.Equal(created.UtcDateTime, stored.CreatedOn);
        Assert.Null(stored.ModifiedBy);
        Assert.Null(stored.ModifiedOn);
    }

    [Fact]
    public async Task Modifying_a_product_stamps_the_modifier_and_keeps_the_creator()
    {
        var id = await AddProductAsync("Widget");
        var created = _db.Clock.Now;

        _db.User.UserName = "bob";
        _db.Clock.Now = created.AddHours(3);
        await using (var context = _db.CreateContext())
        {
            var product = await context.Products.SingleAsync(p => p.Id == id);
            product.Rename("Gadget");
            await context.SaveChangesAsync();
        }

        var stored = await ReadProductAsync(id);
        Assert.Equal("Gadget", stored.ProductName);
        Assert.Equal("alice", stored.CreatedBy);
        Assert.Equal(created.UtcDateTime, stored.CreatedOn);
        Assert.Equal("bob", stored.ModifiedBy);
        Assert.Equal(created.AddHours(3).UtcDateTime, stored.ModifiedOn);
    }

    [Fact]
    public async Task Saving_without_changes_does_not_stamp_a_modification()
    {
        var id = await AddProductAsync("Widget");

        _db.User.UserName = "bob";
        await using (var context = _db.CreateContext())
        {
            await context.Products.SingleAsync(product => product.Id == id);
            await context.SaveChangesAsync();
        }

        var stored = await ReadProductAsync(id);
        Assert.Null(stored.ModifiedBy);
        Assert.Null(stored.ModifiedOn);
    }

    [Fact]
    public async Task Timestamps_are_recorded_to_the_whole_second_so_the_datetime_column_stores_them_exactly()
    {
        _db.Clock.Now = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero).AddTicks(3_984_999);

        var id = await AddProductAsync("Widget");

        Assert.Equal(new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc), (await ReadProductAsync(id)).CreatedOn);
    }

    [Fact]
    public async Task Without_a_signed_in_user_the_system_user_is_recorded()
    {
        _db.User.UserName = null;

        var id = await AddProductAsync("Background job product");

        Assert.Equal(AuditSaveChangesInterceptor.SystemUser, (await ReadProductAsync(id)).CreatedBy);
    }

    [Fact]
    public void Saving_synchronously_stamps_audit_values_too()
    {
        using var context = _db.CreateContext();
        var product = Product.Create("Sync");
        context.Products.Add(product);

        context.SaveChanges();

        Assert.Equal("alice", product.CreatedBy);
    }
}
