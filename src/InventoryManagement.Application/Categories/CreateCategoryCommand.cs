using InventoryManagement.Application.Abstractions;

namespace InventoryManagement.Application.Categories;

public sealed record CreateCategoryCommand(string Name, string? Description) : ICommand<Guid>;
