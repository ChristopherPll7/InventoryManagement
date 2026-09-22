using InventoryManagement.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.Products;
using InventoryManagement.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.Api.Controllers;

[ApiController]
[Route("api/products")]
[ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
[ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
public sealed class ProductsController(
    ICommandHandler<CreateProductCommand, Guid> createHandler,
    IQueryHandler<GetProductByIdQuery, ProductDto?> getByIdHandler,
    IQueryHandler<GetProductsQuery, IReadOnlyCollection<ProductDto>> getAllHandler) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = Permissions.ProductsWrite)]
    public async Task<IActionResult> Create(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand(request.Name, request.Description, request.Sku, request.Price, request.CategoryId);
        var id = await createHandler.HandleAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.ProductsRead)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product = await getByIdHandler.HandleAsync(new GetProductByIdQuery(id), cancellationToken);
        return product is null ? NotFound(new ApiError("PRODUCT_NOT_FOUND", "The product was not found.")) : Ok(product);
    }

    [HttpGet]
    [Authorize(Policy = Permissions.ProductsRead)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await getAllHandler.HandleAsync(new GetProductsQuery(), cancellationToken));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.ProductsWrite)]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, UpdateProductRequest request,
        [FromServices] ICommandHandler<UpdateProductCommand, ProductDto> handler, CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(new UpdateProductCommand(id, request.Name, request.Description, request.Sku, request.Price, request.CategoryId), cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.ProductsWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id,
        [FromServices] ICommandHandler<DeleteProductCommand, CommandCompleted> handler, CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new DeleteProductCommand(id), cancellationToken);
        return NoContent();
    }
}
