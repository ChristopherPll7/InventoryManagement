using InventoryManagement.Application.Abstractions;
using InventoryManagement.Domain.Inventory;

namespace InventoryManagement.Application.Inventory;

public sealed class RegisterInventoryMovementCommandHandler(IInventoryCommandStore store) : ICommandHandler<RegisterInventoryMovementCommand, InventoryMovementResult>
{
    public Task<InventoryMovementResult> HandleAsync(RegisterInventoryMovementCommand command, CancellationToken cancellationToken)
    {
        var movement = InventoryMovement.Create(command.ProductId, command.Type, command.Quantity, command.Reason);
        return store.RegisterAsync(movement, cancellationToken);
    }
}
