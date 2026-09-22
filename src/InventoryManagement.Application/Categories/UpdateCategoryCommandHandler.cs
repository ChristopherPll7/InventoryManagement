using InventoryManagement.Application.Abstractions;
using InventoryManagement.Domain.Common;

namespace InventoryManagement.Application.Categories;

public sealed class UpdateCategoryCommandHandler(ICategoryCommandStore store, ICategoryQueries queries) : ICommandHandler<UpdateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> HandleAsync(UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await queries.GetEntityAsync(command.Id, cancellationToken)
            ?? throw new DomainException("CATEGORY_NOT_FOUND", "The category was not found.");
        category.Update(command.Name, command.Description);
        if (await queries.NameExistsAsync(category.Name, cancellationToken, category.Id))
            throw new DomainException("DUPLICATE_CATEGORY_NAME", "The category name is already in use.");
        return await store.UpdateAsync(category, cancellationToken);
    }
}
