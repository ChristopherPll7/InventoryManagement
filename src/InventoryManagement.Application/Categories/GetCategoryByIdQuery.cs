using InventoryManagement.Application.Abstractions;

namespace InventoryManagement.Application.Categories;

public sealed record GetCategoryByIdQuery(Guid Id) : IQuery<CategoryDto?>;
