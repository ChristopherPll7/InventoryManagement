using InventoryManagement.Application.Abstractions;

namespace InventoryManagement.Application.Categories;

public sealed record DeleteCategoryCommand(Guid Id) : ICommand<CommandCompleted>;
