using InventoryManagement.Application.Abstractions;

namespace InventoryManagement.Application.Categories;

public sealed class GetCategoriesQueryHandler(ICategoryQueries queries) : IQueryHandler<GetCategoriesQuery, IReadOnlyCollection<CategoryDto>>
{
    public Task<IReadOnlyCollection<CategoryDto>> HandleAsync(GetCategoriesQuery query, CancellationToken cancellationToken) =>
        queries.GetAllAsync(cancellationToken);
}
