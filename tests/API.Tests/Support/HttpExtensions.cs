using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProductManagement.Application.DTOs;

namespace ProductManagement.Api.Tests.Support;

public static class HttpExtensions
{
    public const string ProblemJson = "application/problem+json";

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<T>()
        ?? throw new InvalidOperationException($"The response body was empty (expected {typeof(T).Name}).");

    /// <summary>Asserts the response is an RFC 7807 problem with the expected status and returns its body.</summary>
    public static async Task<JsonElement> ReadProblemAsync(this HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)expected, problem.GetProperty("status").GetInt32());
        return problem;
    }

    public static async Task<ProductDto> CreateProductAsync(this HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/products", new CreateProductRequest(name));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadAsync<ProductDto>();
    }

    public static async Task<ItemDto> CreateItemAsync(this HttpClient client, int productId, int quantity)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/products/{productId}/items", new CreateItemRequest(quantity));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadAsync<ItemDto>();
    }
}
