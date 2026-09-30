using InventoryManagement.Application.Abstractions;
using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Products;

namespace InventoryManagement.Application.Products;

public sealed class CreateProductCommandHandler(IProductCommandStore store, IProductValidationQueries queries) : ICommandHandler<CreateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> HandleAsync(CreateProductCommand command, CancellationToken cancellationToken)
    {
        try
        {
            return await CreateAsync(command, cancellationToken);
        }
        catch (DomainException exception)
        {
            // Transactional stores complete rollback before business errors are adapted here.
            return Result<Guid>.Failure(new Error(exception.Code, exception.Message));
        }
    }

    private async Task<Result<Guid>> CreateAsync(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var product = Product.Create(command.Name, command.Description, command.Sku, command.Price, command.CategoryId);
        var categoryIsActive = await queries.GetCategoryActiveStateAsync(product.CategoryId, cancellationToken);
        if (categoryIsActive is null)
            return Result<Guid>.Failure(new Error(ErrorCodes.CategoryNotFound, "The category was not found."));
        if (!categoryIsActive.Value)
            return Result<Guid>.Failure(new Error(ErrorCodes.CategoryInactive, "The category is inactive."));
        if (await queries.SkuExistsAsync(product.Sku, cancellationToken))
            return Result<Guid>.Failure(new Error(ErrorCodes.DuplicateProductSku, "The SKU is already in use."));
        await store.CreateAsync(product, cancellationToken);
        return Result<Guid>.Success(product.Id);
    }
}
