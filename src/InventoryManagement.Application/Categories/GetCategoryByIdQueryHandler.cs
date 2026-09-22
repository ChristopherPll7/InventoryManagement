using InventoryManagement.Application.Abstractions;

namespace InventoryManagement.Application.Categories;

public sealed class GetCategoryByIdQueryHandler(ICategoryQueries queries) : IQueryHandler<GetCategoryByIdQuery, CategoryDto?>
{
    public Task<CategoryDto?> HandleAsync(GetCategoryByIdQuery query, CancellationToken cancellationToken) =>
        queries.GetByIdAsync(query.Id, cancellationToken);
}
