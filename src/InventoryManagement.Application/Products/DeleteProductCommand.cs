using InventoryManagement.Application.Abstractions;

namespace InventoryManagement.Application.Products;

public sealed record DeleteProductCommand(Guid Id) : ICommand<CommandCompleted>;
