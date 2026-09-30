using InventoryManagement.Application.Abstractions;
using InventoryManagement.Domain.Categories;
using InventoryManagement.Domain.Common;

namespace InventoryManagement.Application.Categories;

public sealed class CreateCategoryCommandHandler(ICategoryCommandStore store, ICategoryQueries queries) : ICommandHandler<CreateCategoryCommand, Result<Guid>>
{
    public Task<Result<Guid>> HandleAsync(CreateCategoryCommand command, CancellationToken cancellationToken) =>
        BusinessResult.ExecuteAsync<Guid>(async () =>
        {
            var category = Category.Create(command.Name, command.Description);
            if (await queries.NameExistsAsync(category.Name, cancellationToken))
                return Result<Guid>.Failure(new Error(ErrorCodes.DuplicateCategoryName, "The category name is already in use."));
            await store.CreateAsync(category, cancellationToken);
            return Result<Guid>.Success(category.Id);
        });
}
