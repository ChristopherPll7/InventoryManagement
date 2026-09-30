using InventoryManagement.Application.Abstractions;
using InventoryManagement.Domain.Inventory;

namespace InventoryManagement.Application.Tests;

internal sealed class InventoryCommandStoreStub : IInventoryCommandStore
{
    public Exception? Error { get; init; }
    public int Calls { get; private set; }

    public Task<InventoryMovementResult> RegisterAsync(InventoryMovement movement, CancellationToken cancellationToken)
    {
        Calls++;
        return Error is null
            ? Task.FromResult(new InventoryMovementResult(movement.Id, 5))
            : Task.FromException<InventoryMovementResult>(Error);
    }
}
