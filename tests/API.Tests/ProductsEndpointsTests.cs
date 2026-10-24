using System.Net;
using System.Net.Http.Json;
using ProductManagement.Api.Tests.Support;
using ProductManagement.Application.DTOs;
using ProductManagement.Domain.Entities;

namespace ProductManagement.Api.Tests;

public sealed class ProductsEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Products = "/api/v1/products";

    [Fact]
    public async Task Requests_without_a_token_are_rejected_with_a_problem_response()
    {
        var client = factory.CreateClient();

        foreach (var response in new[]
        {
            await client.GetAsync(Products),
            await client.GetAsync($"{Products}/1"),
            await client.PostAsJsonAsync(Products, new CreateProductRequest("Widget")),
            await client.DeleteAsync($"{Products}/1")
        })
        {
            await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task Create_returns_201_with_a_location_and_records_who_created_it()
    {
        var user = await factory.CreateUserAsync();

        var response = await user.Client.PostAsJsonAsync(Products, new CreateProductRequest("  Widget  "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.ReadAsync<ProductDto>();
        Assert.Equal("Widget", product.ProductName);
        Assert.Equal(user.Email, product.CreatedBy);
        Assert.Equal(DateTimeKind.Utc, product.CreatedOn.Kind);
        Assert.InRange(product.CreatedOn, DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddMinutes(1));
        Assert.Null(product.ModifiedBy);
        Assert.Null(product.ModifiedOn);
        Assert.EndsWith($"{Products}/{product.Id}", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task A_created_product_can_be_fetched_by_id()
    {
        var user = await factory.CreateUserAsync();
        var created = await user.Client.CreateProductAsync("Fetch me");

        var response = await user.Client.GetAsync($"{Products}/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created, await response.ReadAsync<ProductDto>());
    }

    [Fact]
    public async Task Getting_an_unknown_product_returns_404()
    {
        var user = await factory.CreateUserAsync();

        var response = await user.Client.GetAsync($"{Products}/999999");

        await response.ReadProblemAsync(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_with_a_blank_name_returns_400(string name)
    {
        var user = await factory.CreateUserAsync();

        var response = await user.Client.PostAsJsonAsync(Products, new CreateProductRequest(name));

        var problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.True(problem.GetProperty("errors").TryGetProperty("productName", out _));
    }

    [Fact]
    public async Task Create_with_a_name_longer_than_the_column_returns_400()
    {
        var user = await factory.CreateUserAsync();

        var response = await user.Client.PostAsJsonAsync(
            Products, new CreateProductRequest(new string('a', Product.NameMaxLength + 1)));

        await response.ReadProblemAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_with_a_name_of_exactly_the_maximum_length_succeeds()
    {
        var user = await factory.CreateUserAsync();

        var product = await user.Client.CreateProductAsync(new string('a', Product.NameMaxLength));

        Assert.Equal(Product.NameMaxLength, product.ProductName.Length);
    }

    [Fact]
    public async Task Create_with_an_empty_json_object_returns_400()
    {
        var user = await factory.CreateUserAsync();

        var response = await user.Client.PostAsync(Products, new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        await response.ReadProblemAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_changes_the_name_and_records_who_modified_it()
    {
        var creator = await factory.CreateUserAsync();
        var editor = await factory.CreateUserAsync();
        var created = await creator.Client.CreateProductAsync("Before");

        var response = await editor.Client.PutAsJsonAsync($"{Products}/{created.Id}", new UpdateProductRequest("After"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.ReadAsync<ProductDto>();
        Assert.Equal("After", updated.ProductName);
        Assert.Equal(creator.Email, updated.CreatedBy);
        Assert.Equal(created.CreatedOn, updated.CreatedOn);
        Assert.Equal(editor.Email, updated.ModifiedBy);
        Assert.NotNull(updated.ModifiedOn);

        var fetched = await (await creator.Client.GetAsync($"{Products}/{created.Id}")).ReadAsync<ProductDto>();
        Assert.Equal(updated, fetched);
    }

    [Fact]
    public async Task Update_with_a_blank_name_returns_400_and_changes_nothing()
    {
        var user = await factory.CreateUserAsync();
        var created = await user.Client.CreateProductAsync("Keep me");

        var response = await user.Client.PutAsJsonAsync($"{Products}/{created.Id}", new UpdateProductRequest(""));

        await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        var fetched = await (await user.Client.GetAsync($"{Products}/{created.Id}")).ReadAsync<ProductDto>();
        Assert.Equal("Keep me", fetched.ProductName);
    }

    [Fact]
    public async Task Updating_an_unknown_product_returns_404()
    {
        var user = await factory.CreateUserAsync();

        var response = await user.Client.PutAsJsonAsync($"{Products}/999999", new UpdateProductRequest("Name"));

        await response.ReadProblemAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task The_list_is_paged_and_reports_the_totals()
    {
        var user = await factory.CreateUserAsync();
        for (var number = 1; number <= 3; number++)
        {
            await user.Client.CreateProductAsync($"Paged {number}");
        }

        var response = await user.Client.GetAsync($"{Products}?pageNumber=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.ReadAsync<PagedResult<ProductDto>>();
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(1, page.PageNumber);
        Assert.Equal(2, page.PageSize);
        Assert.True(page.TotalCount >= 3);
        Assert.Equal((int)Math.Ceiling(page.TotalCount / 2.0), page.TotalPages);
        Assert.True(page.Items[0].Id < page.Items[1].Id);
    }

    [Fact]
    public async Task The_list_uses_sensible_paging_defaults()
    {
        var user = await factory.CreateUserAsync();

        var page = await (await user.Client.GetAsync(Products)).ReadAsync<PagedResult<ProductDto>>();

        Assert.Equal(1, page.PageNumber);
        Assert.Equal(PagedQuery.DefaultPageSize, page.PageSize);
    }

    [Theory]
    [InlineData("pageNumber=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    [InlineData("pageNumber=abc")]
    public async Task The_list_rejects_invalid_paging(string query)
    {
        var user = await factory.CreateUserAsync();

        var response = await user.Client.GetAsync($"{Products}?{query}");

        await response.ReadProblemAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_regular_user_cannot_delete_a_product()
    {
        var user = await factory.CreateUserAsync();
        var created = await user.Client.CreateProductAsync("Protected");

        var response = await user.Client.DeleteAsync($"{Products}/{created.Id}");

        await response.ReadProblemAsync(HttpStatusCode.Forbidden);
        Assert.Equal(HttpStatusCode.OK, (await user.Client.GetAsync($"{Products}/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task An_administrator_can_delete_a_product()
    {
        var admin = await factory.CreateAdminAsync();
        var created = await admin.Client.CreateProductAsync("Disposable");

        var response = await admin.Client.DeleteAsync($"{Products}/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await (await admin.Client.GetAsync($"{Products}/{created.Id}")).ReadProblemAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Deleting_an_unknown_product_returns_404()
    {
        var admin = await factory.CreateAdminAsync();

        var response = await admin.Client.DeleteAsync($"{Products}/999999");

        await response.ReadProblemAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_product_with_items_cannot_be_deleted_until_its_items_are()
    {
        var admin = await factory.CreateAdminAsync();
        var product = await admin.Client.CreateProductAsync("Has stock");
        var item = await admin.Client.CreateItemAsync(product.Id, 5);

        var blocked = await admin.Client.DeleteAsync($"{Products}/{product.Id}");
        await blocked.ReadProblemAsync(HttpStatusCode.Conflict);
        Assert.Equal(HttpStatusCode.OK, (await admin.Client.GetAsync($"{Products}/{product.Id}")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.Client.DeleteAsync($"{Products}/{product.Id}/items/{item.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.Client.DeleteAsync($"{Products}/{product.Id}")).StatusCode);
    }
}
