using System.Net;
using System.Net.Http.Json;
using ProductManagement.Api.Tests.Support;
using ProductManagement.Application.DTOs;

namespace ProductManagement.Api.Tests;

public sealed class ItemsEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static string ItemsOf(int productId) => $"/api/v1/products/{productId}/items";

    [Fact]
    public async Task Requests_without_a_token_are_rejected()
    {
        var response = await factory.CreateClient().GetAsync(ItemsOf(1));

        await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task An_item_can_be_created_read_updated_and_deleted_under_its_product()
    {
        var user = await factory.CreateUserAsync();
        var product = await user.Client.CreateProductAsync("With items");

        var createResponse = await user.Client.PostAsJsonAsync(ItemsOf(product.Id), new CreateItemRequest(7));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.ReadAsync<ItemDto>();
        Assert.Equal(product.Id, created.ProductId);
        Assert.Equal(7, created.Quantity);
        Assert.EndsWith($"{ItemsOf(product.Id)}/{created.Id}", createResponse.Headers.Location?.AbsolutePath);

        var fetched = await (await user.Client.GetAsync($"{ItemsOf(product.Id)}/{created.Id}")).ReadAsync<ItemDto>();
        Assert.Equal(created, fetched);

        var updateResponse = await user.Client.PutAsJsonAsync($"{ItemsOf(product.Id)}/{created.Id}", new UpdateItemRequest(9));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal(9, (await updateResponse.ReadAsync<ItemDto>()).Quantity);

        var deleteResponse = await user.Client.DeleteAsync($"{ItemsOf(product.Id)}/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        await (await user.Client.GetAsync($"{ItemsOf(product.Id)}/{created.Id}")).ReadProblemAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Creating_an_item_for_an_unknown_product_returns_404()
    {
        var user = await factory.CreateUserAsync();

        var response = await user.Client.PostAsJsonAsync(ItemsOf(999999), new CreateItemRequest(1));

        await response.ReadProblemAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Listing_the_items_of_an_unknown_product_returns_404()
    {
        var user = await factory.CreateUserAsync();

        var response = await user.Client.GetAsync(ItemsOf(999999));

        await response.ReadProblemAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_negative_quantity_returns_400()
    {
        var user = await factory.CreateUserAsync();
        var product = await user.Client.CreateProductAsync("Strict");

        var create = await user.Client.PostAsJsonAsync(ItemsOf(product.Id), new CreateItemRequest(-1));
        var item = await user.Client.CreateItemAsync(product.Id, 1);
        var update = await user.Client.PutAsJsonAsync($"{ItemsOf(product.Id)}/{item.Id}", new UpdateItemRequest(-1));

        var problem = await create.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.True(problem.GetProperty("errors").TryGetProperty("quantity", out _));
        await update.ReadProblemAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_quantity_of_zero_is_allowed()
    {
        var user = await factory.CreateUserAsync();
        var product = await user.Client.CreateProductAsync("Empty shelf");

        var item = await user.Client.CreateItemAsync(product.Id, 0);

        Assert.Equal(0, item.Quantity);
    }

    [Fact]
    public async Task An_item_cannot_be_reached_through_another_product()
    {
        var user = await factory.CreateUserAsync();
        var owner = await user.Client.CreateProductAsync("Owner");
        var stranger = await user.Client.CreateProductAsync("Stranger");
        var item = await user.Client.CreateItemAsync(owner.Id, 3);

        var get = await user.Client.GetAsync($"{ItemsOf(stranger.Id)}/{item.Id}");
        var put = await user.Client.PutAsJsonAsync($"{ItemsOf(stranger.Id)}/{item.Id}", new UpdateItemRequest(1));
        var delete = await user.Client.DeleteAsync($"{ItemsOf(stranger.Id)}/{item.Id}");

        await get.ReadProblemAsync(HttpStatusCode.NotFound);
        await put.ReadProblemAsync(HttpStatusCode.NotFound);
        await delete.ReadProblemAsync(HttpStatusCode.NotFound);
        Assert.Equal(3, (await (await user.Client.GetAsync($"{ItemsOf(owner.Id)}/{item.Id}")).ReadAsync<ItemDto>()).Quantity);
    }

    [Fact]
    public async Task The_item_list_is_paged_and_contains_only_that_products_items()
    {
        var user = await factory.CreateUserAsync();
        var product = await user.Client.CreateProductAsync("Shelf");
        var other = await user.Client.CreateProductAsync("Other shelf");
        foreach (var quantity in new[] { 1, 2, 3 })
        {
            await user.Client.CreateItemAsync(product.Id, quantity);
        }
        await user.Client.CreateItemAsync(other.Id, 99);

        var first = await (await user.Client.GetAsync($"{ItemsOf(product.Id)}?pageNumber=1&pageSize=2")).ReadAsync<PagedResult<ItemDto>>();
        var second = await (await user.Client.GetAsync($"{ItemsOf(product.Id)}?pageNumber=2&pageSize=2")).ReadAsync<PagedResult<ItemDto>>();

        Assert.Equal(new[] { 1, 2 }, first.Items.Select(item => item.Quantity).ToArray());
        Assert.Equal(new[] { 3 }, second.Items.Select(item => item.Quantity).ToArray());
        Assert.Equal(3, first.TotalCount);
        Assert.Equal(2, first.TotalPages);
        Assert.All(first.Items.Concat(second.Items), item => Assert.Equal(product.Id, item.ProductId));
    }
}
