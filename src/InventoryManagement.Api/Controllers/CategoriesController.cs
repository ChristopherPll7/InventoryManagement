using InventoryManagement.Domain.Common;
using InventoryManagement.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using InventoryManagement.Api.Contracts;
using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.Categories;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.Api.Controllers;

[ApiController]
[Route("api/categories")]
[ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
[ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
public sealed class CategoriesController : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = Permissions.CategoriesWrite)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CategoryRequest request, [FromServices] ICommandHandler<CreateCategoryCommand, Result<Guid>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new CreateCategoryCommand(request.Name, request.Description), cancellationToken);
        if (result.IsFailure)
            return BusinessErrorResponse.From(result.Error);
        var id = result.Value;
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.CategoriesRead)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, [FromServices] IQueryHandler<GetCategoryByIdQuery, Result<CategoryDto>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetCategoryByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BusinessErrorResponse.From(result.Error);
    }

    [HttpGet]
    [Authorize(Policy = Permissions.CategoriesRead)]
    [ProducesResponseType(typeof(PagedResult<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromServices] IQueryHandler<GetCategoriesQuery, Result<PagedResult<CategoryDto>>> handler, CancellationToken cancellationToken, [FromQuery] int page = 1, [FromQuery] int pageSize = PageRequest.DefaultPageSize)
    {
        var result = await handler.HandleAsync(new GetCategoriesQuery(page, pageSize), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BusinessErrorResponse.From(result.Error);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.CategoriesWrite)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, CategoryRequest request, [FromServices] ICommandHandler<UpdateCategoryCommand, Result<CategoryDto>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new UpdateCategoryCommand(id, request.Name, request.Description), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BusinessErrorResponse.From(result.Error);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.CategoriesWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, [FromServices] ICommandHandler<DeleteCategoryCommand, Result<CommandCompleted>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteCategoryCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : BusinessErrorResponse.From(result.Error);
    }
}
