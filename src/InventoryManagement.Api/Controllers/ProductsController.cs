using InventoryManagement.Domain.Common;
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
    ICommandHandler<CreateProductCommand, Result<Guid>> createHandler,
    IQueryHandler<GetProductByIdQuery, Result<ProductDto>> getByIdHandler,
    IQueryHandler<GetProductsQuery, Result<PagedResult<ProductDto>>> getAllHandler) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = Permissions.ProductsWrite)]
    public async Task<IActionResult> Create(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand(request.Name, request.Description, request.Sku, request.Price, request.CategoryId);
        var result = await createHandler.HandleAsync(command, cancellationToken);
        if (result.IsFailure)
            return BusinessErrorResponse.From(result.Error);
        var id = result.Value;
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.ProductsRead)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await getByIdHandler.HandleAsync(new GetProductByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BusinessErrorResponse.From(result.Error);
    }

    [HttpGet]
    [Authorize(Policy = Permissions.ProductsRead)]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken, [FromQuery] int page = 1, [FromQuery] int pageSize = PageRequest.DefaultPageSize)
    {
        var result = await getAllHandler.HandleAsync(new GetProductsQuery(page, pageSize), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BusinessErrorResponse.From(result.Error);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.ProductsWrite)]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, UpdateProductRequest request,
        [FromServices] ICommandHandler<UpdateProductCommand, Result<ProductDto>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new UpdateProductCommand(id, request.Name, request.Description, request.Sku, request.Price, request.CategoryId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BusinessErrorResponse.From(result.Error);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.ProductsWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id,
        [FromServices] ICommandHandler<DeleteProductCommand, Result<CommandCompleted>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteProductCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : BusinessErrorResponse.From(result.Error);
    }
}
