using InventoryManagement.Application.Abstractions;
using InventoryManagement.Domain.Inventory;

namespace InventoryManagement.Application.Inventory;

public sealed class RegisterInventoryMovementCommandHandler(IInventoryCommandStore store) : ICommandHandler<RegisterInventoryMovementCommand, Result<InventoryMovementResult>>
{
    public Task<Result<InventoryMovementResult>> HandleAsync(RegisterInventoryMovementCommand command, CancellationToken cancellationToken) =>
        BusinessResult.ExecuteAsync<InventoryMovementResult>(async () =>
        {
            var movement = InventoryMovement.Create(command.ProductId, command.Type, command.Quantity, command.Reason);
            return Result<InventoryMovementResult>.Success(await store.RegisterAsync(movement, cancellationToken));
        });
}
