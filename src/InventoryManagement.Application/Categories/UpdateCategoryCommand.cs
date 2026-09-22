using InventoryManagement.Application.Abstractions;

namespace InventoryManagement.Application.Categories;

public sealed record UpdateCategoryCommand(Guid Id, string Name, string? Description) : ICommand<CategoryDto>;
