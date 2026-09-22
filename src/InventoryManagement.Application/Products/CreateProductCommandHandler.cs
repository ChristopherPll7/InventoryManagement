using InventoryManagement.Application.Abstractions;
using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Products;

namespace InventoryManagement.Application.Products;

public sealed class CreateProductCommandHandler(IProductCommandStore store, IProductValidationQueries queries) : ICommandHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var product = Product.Create(command.Name, command.Description, command.Sku, command.Price, command.CategoryId);
        var categoryIsActive = await queries.GetCategoryActiveStateAsync(product.CategoryId, cancellationToken);
        if (categoryIsActive is null)
            throw new DomainException("CATEGORY_NOT_FOUND", "The category was not found.");
        if (!categoryIsActive.Value)
            throw new DomainException("CATEGORY_INACTIVE", "The category is inactive.");
        if (await queries.SkuExistsAsync(product.Sku, cancellationToken))
            throw new DomainException("DUPLICATE_PRODUCT_SKU", "The SKU is already in use.");
        await store.CreateAsync(product, cancellationToken);
        return product.Id;
    }
}
