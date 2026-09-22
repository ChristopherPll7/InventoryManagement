using InventoryManagement.Application.Abstractions;
using InventoryManagement.Domain.Inventory;

namespace InventoryManagement.Application.Inventory;

public sealed record RegisterInventoryMovementCommand(Guid ProductId, InventoryMovementType Type, int Quantity, string? Reason) : ICommand<InventoryMovementResult>;
