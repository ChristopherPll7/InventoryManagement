using InventoryManagement.Domain.Inventory;

namespace InventoryManagement.Application.Abstractions;

public sealed record InventoryMovementDto(Guid Id, Guid ProductId, InventoryMovementType Type, int Quantity, string? Reason, DateTimeOffset CreatedAt);
