using InventoryManagement.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.Inventory;
using InventoryManagement.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.Api.Controllers;

[ApiController]
[Route("api/inventory")]
[ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
[ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
public sealed class InventoryController(ICommandHandler<RegisterInventoryMovementCommand, Result<InventoryMovementResult>> handler) : ControllerBase
{
    [HttpPost("movements")]
    [Authorize(Policy = Permissions.InventoryWrite)]
    public async Task<IActionResult> Register(RegisterInventoryMovementRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterInventoryMovementCommand(request.ProductId, request.Type, request.Quantity, request.Reason);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : BusinessErrorResponse.From(result.Error);
    }

    [HttpGet("products/{productId:guid}")]
    [Authorize(Policy = Permissions.InventoryRead)]
    [ProducesResponseType(typeof(ProductInventoryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductInventory(Guid productId,
        [FromServices] IQueryHandler<GetProductInventoryQuery, Result<ProductInventoryDto>> queryHandler, CancellationToken cancellationToken)
    {
        var result = await queryHandler.HandleAsync(new GetProductInventoryQuery(productId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BusinessErrorResponse.From(result.Error);
    }

    [HttpGet("products/{productId:guid}/movements")]
    [Authorize(Policy = Permissions.InventoryRead)]
    [ProducesResponseType(typeof(PagedResult<InventoryMovementDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMovements(Guid productId, [FromQuery] InventoryMovementHistoryRequest request,
        [FromServices] IQueryHandler<GetProductInventoryMovementsQuery, Result<PagedResult<InventoryMovementDto>>> queryHandler, CancellationToken cancellationToken)
    {
        var result = await queryHandler.HandleAsync(request.ToQuery(productId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BusinessErrorResponse.From(result.Error);
    }
}
