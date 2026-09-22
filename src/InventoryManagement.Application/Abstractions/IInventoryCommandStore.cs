using InventoryManagement.Domain.Inventory;

namespace InventoryManagement.Application.Abstractions;

public interface IInventoryCommandStore
{
    Task<InventoryMovementResult> RegisterAsync(InventoryMovement movement, CancellationToken cancellationToken);
}
