using InventoryManagement.Application.Abstractions;
using InventoryManagement.Domain.Common;

namespace InventoryManagement.Application.Products;

public sealed class UpdateProductCommandHandler(IProductCommandStore store, IProductValidationQueries queries) : ICommandHandler<UpdateProductCommand, ProductDto>
{
    public async Task<ProductDto> HandleAsync(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await queries.GetProductAsync(command.Id, cancellationToken)
            ?? throw new DomainException("PRODUCT_NOT_FOUND", "The product was not found.");
        product.Update(command.Name, command.Description, command.Sku, command.Price, command.CategoryId);
        var categoryIsActive = await queries.GetCategoryActiveStateAsync(product.CategoryId, cancellationToken);
        if (categoryIsActive is null)
            throw new DomainException("CATEGORY_NOT_FOUND", "The category was not found.");
        if (!categoryIsActive.Value)
            throw new DomainException("CATEGORY_INACTIVE", "The category is inactive.");
        if (await queries.SkuExistsAsync(product.Sku, cancellationToken, product.Id))
            throw new DomainException("DUPLICATE_PRODUCT_SKU", "The SKU is already in use.");
        return await store.UpdateAsync(product, cancellationToken);
    }
}
