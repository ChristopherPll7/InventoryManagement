using InventoryManagement.Application.Abstractions;

namespace InventoryManagement.Application.Categories;

public sealed class DeleteCategoryCommandHandler(ICategoryCommandStore store) : ICommandHandler<DeleteCategoryCommand, CommandCompleted>
{
    public async Task<CommandCompleted> HandleAsync(DeleteCategoryCommand command, CancellationToken cancellationToken)
    {
        await store.DeactivateAsync(command.Id, cancellationToken);
        return new CommandCompleted();
    }
}
