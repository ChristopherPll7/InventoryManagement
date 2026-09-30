using InventoryManagement.Application.Abstractions;
using InventoryManagement.Domain.Products;

namespace InventoryManagement.Application.Products;

public sealed class UpdateProductCommandHandler(IProductCommandStore store) : ICommandHandler<UpdateProductCommand, Result<ProductDto>>
{
    public Task<Result<ProductDto>> HandleAsync(UpdateProductCommand command, CancellationToken cancellationToken) =>
        BusinessResult.ExecuteAsync<ProductDto>(async () =>
        {
            var details = new ProductDetails(command.Name, command.Description, command.Sku, command.Price, command.CategoryId);
            return Result<ProductDto>.Success(await store.UpdateAsync(command.Id, details, cancellationToken));
        });
}
