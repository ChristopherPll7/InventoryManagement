namespace InventoryManagement.Application.Abstractions;

public sealed record InventoryMovementResult(Guid MovementId, int CurrentStock);
