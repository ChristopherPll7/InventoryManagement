using InventoryManagement.Application.Abstractions;

namespace InventoryManagement.Application.Products;

public sealed class DeleteProductCommandHandler(IProductCommandStore store) : ICommandHandler<DeleteProductCommand, CommandCompleted>
{
    public async Task<CommandCompleted> HandleAsync(DeleteProductCommand command, CancellationToken cancellationToken)
    {
        await store.DeactivateAsync(command.Id, cancellationToken);
        return new CommandCompleted();
    }
}
