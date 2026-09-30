using InventoryManagement.Application.Abstractions;

namespace InventoryManagement.Application.Categories;

public sealed class GetCategoriesQueryHandler(ICategoryQueries queries) : IQueryHandler<GetCategoriesQuery, Result<PagedResult<CategoryDto>>>
{
    public Task<Result<PagedResult<CategoryDto>>> HandleAsync(GetCategoriesQuery query, CancellationToken cancellationToken) =>
        BusinessResult.ExecuteAsync<PagedResult<CategoryDto>>(async () =>
        {
            return Result<PagedResult<CategoryDto>>.Success(await queries.GetAllAsync(new PageRequest(query.Page, query.PageSize), cancellationToken));
        });
}
