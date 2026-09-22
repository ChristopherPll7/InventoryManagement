using InventoryManagement.Application.Abstractions;

namespace InventoryManagement.Application.Categories;

public sealed record GetCategoriesQuery() : IQuery<IReadOnlyCollection<CategoryDto>>;
