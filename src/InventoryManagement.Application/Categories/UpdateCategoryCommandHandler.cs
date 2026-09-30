using InventoryManagement.Application.Abstractions;
using InventoryManagement.Domain.Common;

namespace InventoryManagement.Application.Categories;

public sealed class UpdateCategoryCommandHandler(ICategoryCommandStore store, ICategoryQueries queries) : ICommandHandler<UpdateCategoryCommand, Result<CategoryDto>>
{
    public Task<Result<CategoryDto>> HandleAsync(UpdateCategoryCommand command, CancellationToken cancellationToken) =>
        BusinessResult.ExecuteAsync<CategoryDto>(async () =>
        {
            var category = await queries.GetEntityAsync(command.Id, cancellationToken);
            if (category is null)
                return Result<CategoryDto>.Failure(new Error(ErrorCodes.CategoryNotFound, "The category was not found."));
            category.Update(command.Name, command.Description);
            if (await queries.NameExistsAsync(category.Name, cancellationToken, category.Id))
                return Result<CategoryDto>.Failure(new Error(ErrorCodes.DuplicateCategoryName, "The category name is already in use."));
            return Result<CategoryDto>.Success(await store.UpdateAsync(category, cancellationToken));
        });
}
