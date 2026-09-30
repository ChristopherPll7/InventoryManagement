using InventoryManagement.Domain.Common;
using InventoryManagement.Application.Abstractions;

namespace InventoryManagement.Application.Categories;

public sealed class GetCategoryByIdQueryHandler(ICategoryQueries queries) : IQueryHandler<GetCategoryByIdQuery, Result<CategoryDto>>
{
    public Task<Result<CategoryDto>> HandleAsync(GetCategoryByIdQuery query, CancellationToken cancellationToken) =>
        BusinessResult.ExecuteAsync<CategoryDto>(async () =>
        {
            var item = await queries.GetByIdAsync(query.Id, cancellationToken);
            return item is null
                ? Result<CategoryDto>.Failure(new Error(ErrorCodes.CategoryNotFound, "The category was not found."))
                : Result<CategoryDto>.Success(item);
        });
}
