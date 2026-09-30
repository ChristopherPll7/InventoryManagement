using InventoryManagement.Application.Abstractions;

namespace InventoryManagement.Application.Categories;

public sealed class DeleteCategoryCommandHandler(ICategoryCommandStore store) : ICommandHandler<DeleteCategoryCommand, Result<CommandCompleted>>
{
    public Task<Result<CommandCompleted>> HandleAsync(DeleteCategoryCommand command, CancellationToken cancellationToken) =>
        BusinessResult.ExecuteAsync<CommandCompleted>(async () =>
        {
            await store.DeactivateAsync(command.Id, cancellationToken);
            return Result<CommandCompleted>.Success(new CommandCompleted());
        });
}
