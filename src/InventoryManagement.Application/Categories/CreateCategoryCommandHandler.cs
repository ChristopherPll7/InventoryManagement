using InventoryManagement.Application.Abstractions;
using InventoryManagement.Domain.Categories;
using InventoryManagement.Domain.Common;

namespace InventoryManagement.Application.Categories;

public sealed class CreateCategoryCommandHandler(ICategoryCommandStore store, ICategoryQueries queries) : ICommandHandler<CreateCategoryCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = Category.Create(command.Name, command.Description);
        if (await queries.NameExistsAsync(category.Name, cancellationToken))
            throw new DomainException("DUPLICATE_CATEGORY_NAME", "The category name is already in use.");
        await store.CreateAsync(category, cancellationToken);
        return category.Id;
    }
}
