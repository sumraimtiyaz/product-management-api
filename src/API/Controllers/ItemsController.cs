using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using ProductManagement.Application.DTOs;
using ProductManagement.Application.Interfaces;

namespace ProductManagement.Api.Controllers;

/// <summary>Create, read, update and delete the items of a product.</summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/products/{productId:int}/items")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
public sealed class ItemsController(IItemService items) : ApiControllerBase
{
    /// <summary>Lists the items of a product, one page at a time.</summary>
    /// <param name="productId">The product id.</param>
    /// <param name="query">Paging options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ItemDto>>> GetAll(
        int productId, [FromQuery] PagedQuery query, CancellationToken cancellationToken) =>
        Ok(await items.GetPagedAsync(productId, query, cancellationToken));

    /// <summary>Gets one item of a product.</summary>
    /// <param name="productId">The product id.</param>
    /// <param name="itemId">The item id.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    [HttpGet("{itemId:int}")]
    [ProducesResponseType(typeof(ItemDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ItemDto>> GetById(int productId, int itemId, CancellationToken cancellationToken) =>
        Ok(await items.GetByIdAsync(productId, itemId, cancellationToken));

    /// <summary>Creates an item under a product.</summary>
    /// <param name="productId">The product id.</param>
    /// <param name="request">The item to create.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    [HttpPost]
    [ProducesResponseType(typeof(ItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ItemDto>> Create(
        int productId, CreateItemRequest request, CancellationToken cancellationToken)
    {
        var created = await items.CreateAsync(productId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { productId, itemId = created.Id }, created);
    }

    /// <summary>Updates an item of a product.</summary>
    /// <param name="productId">The product id.</param>
    /// <param name="itemId">The item id.</param>
    /// <param name="request">The new item data.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    [HttpPut("{itemId:int}")]
    [ProducesResponseType(typeof(ItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ItemDto>> Update(
        int productId, int itemId, UpdateItemRequest request, CancellationToken cancellationToken) =>
        Ok(await items.UpdateAsync(productId, itemId, request, cancellationToken));

    /// <summary>Deletes an item of a product.</summary>
    /// <param name="productId">The product id.</param>
    /// <param name="itemId">The item id.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    [HttpDelete("{itemId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int productId, int itemId, CancellationToken cancellationToken)
    {
        await items.DeleteAsync(productId, itemId, cancellationToken);
        return NoContent();
    }
}
