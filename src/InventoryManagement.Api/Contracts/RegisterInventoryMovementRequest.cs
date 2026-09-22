using InventoryManagement.Domain.Inventory;

namespace InventoryManagement.Api.Contracts;

public sealed class RegisterInventoryMovementRequest
{
    public required Guid ProductId { get; init; }
    public required InventoryMovementType Type { get; init; }
    public required int Quantity { get; init; }
    public string? Reason { get; init; }
}
