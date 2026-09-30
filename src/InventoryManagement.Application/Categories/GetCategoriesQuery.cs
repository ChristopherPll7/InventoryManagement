using InventoryManagement.Application.Abstractions;

namespace InventoryManagement.Application.Categories;

public sealed record GetCategoriesQuery(int Page = 1, int PageSize = PageRequest.DefaultPageSize) : IQuery<Result<PagedResult<CategoryDto>>>;
