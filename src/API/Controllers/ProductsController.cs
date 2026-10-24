using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductManagement.Application.DTOs;
using ProductManagement.Application.Interfaces;
using ProductManagement.Domain.Enums;

namespace ProductManagement.Api.Controllers;

/// <summary>Create, read, update and delete products.</summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/products")]
public sealed class ProductsController(IProductService products) : ApiControllerBase
{
    /// <summary>Lists products, one page at a time.</summary>
    /// <param name="query">Paging options.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetAll(
        [FromQuery] PagedQuery query, CancellationToken cancellationToken) =>
        Ok(await products.GetPagedAsync(query, cancellationToken));

    /// <summary>Gets one product.</summary>
    /// <param name="id">The product id.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await products.GetByIdAsync(id, cancellationToken));

    /// <summary>Creates a product. The creating user is recorded in <c>createdBy</c>.</summary>
    /// <param name="request">The product to create.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDto>> Create(
        CreateProductRequest request, CancellationToken cancellationToken)
    {
        var created = await products.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Updates a product. The updating user is recorded in <c>modifiedBy</c>.</summary>
    /// <param name="id">The product id.</param>
    /// <param name="request">The new product data.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> Update(
        int id, UpdateProductRequest request, CancellationToken cancellationToken) =>
        Ok(await products.UpdateAsync(id, request, cancellationToken));

    /// <summary>Deletes a product that has no items. Administrators only.</summary>
    /// <param name="id">The product id.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await products.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
