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
    public async Task<IActionResult> Create(CategoryRequest request, [FromServices] ICommandHandler<CreateCategoryCommand, Guid> handler, CancellationToken cancellationToken)
    {
        var id = await handler.HandleAsync(new CreateCategoryCommand(request.Name, request.Description), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.CategoriesRead)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, [FromServices] IQueryHandler<GetCategoryByIdQuery, CategoryDto?> handler, CancellationToken cancellationToken)
    {
        var category = await handler.HandleAsync(new GetCategoryByIdQuery(id), cancellationToken);
        return category is null ? NotFound(new ApiError("CATEGORY_NOT_FOUND", "The category was not found.")) : Ok(category);
    }

    [HttpGet]
    [Authorize(Policy = Permissions.CategoriesRead)]
    [ProducesResponseType(typeof(IReadOnlyCollection<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromServices] IQueryHandler<GetCategoriesQuery, IReadOnlyCollection<CategoryDto>> handler, CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(new GetCategoriesQuery(), cancellationToken));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.CategoriesWrite)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, CategoryRequest request, [FromServices] ICommandHandler<UpdateCategoryCommand, CategoryDto> handler, CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(new UpdateCategoryCommand(id, request.Name, request.Description), cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.CategoriesWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, [FromServices] ICommandHandler<DeleteCategoryCommand, CommandCompleted> handler, CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new DeleteCategoryCommand(id), cancellationToken);
        return NoContent();
    }
}
