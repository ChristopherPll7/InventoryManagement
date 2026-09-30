using InventoryManagement.Application.Abstractions;

namespace InventoryManagement.Application.Products;

public sealed class DeleteProductCommandHandler(IProductCommandStore store) : ICommandHandler<DeleteProductCommand, Result<CommandCompleted>>
{
    public Task<Result<CommandCompleted>> HandleAsync(DeleteProductCommand command, CancellationToken cancellationToken) =>
        BusinessResult.ExecuteAsync<CommandCompleted>(async () =>
        {
            await store.DeactivateAsync(command.Id, cancellationToken);
            return Result<CommandCompleted>.Success(new CommandCompleted());
        });
}
