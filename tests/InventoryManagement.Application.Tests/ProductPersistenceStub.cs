using InventoryManagement.Application.Abstractions;
using InventoryManagement.Domain.Products;
using InventoryManagement.Domain.Common;

namespace InventoryManagement.Application.Tests;

internal sealed class ProductPersistenceStub : IProductCommandStore, IProductValidationQueries
{
    public bool? CategoryIsActive { get; init; } = true;
    public bool DuplicateSku { get; init; }
    public Product? CreatedProduct { get; private set; }
    public string? CheckedSku { get; private set; }
    public int ReadCount { get; private set; }
    public Exception? UpdateError { get; init; }
    public ProductDetails? ReceivedDetails { get; private set; }
    public Exception? CreateError { get; init; }
    public Product? ExistingProduct { get; init; }
    public Product? UpdatedProduct { get; private set; }
    public Guid? ExcludedProductId { get; private set; }

    public Task<bool?> GetCategoryActiveStateAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        ReadCount++;
        return Task.FromResult(CategoryIsActive);
    }

    public Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken, Guid? excludingProductId = null)
    {
        ReadCount++;
        CheckedSku = sku;
        ExcludedProductId = excludingProductId;
        return Task.FromResult(DuplicateSku);
    }

    public Task CreateAsync(Product product, CancellationToken cancellationToken)
    {
        if (CreateError is not null)
            return Task.FromException(CreateError);
        CreatedProduct = product;
        return Task.CompletedTask;
    }

    public Task<Product?> GetProductAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(ExistingProduct?.Id == id ? ExistingProduct : null);

    public Task<ProductDto> UpdateAsync(Guid id, ProductDetails details, CancellationToken cancellationToken)
    {
        if (UpdateError is not null)
            return Task.FromException<ProductDto>(UpdateError);
        if (ExistingProduct?.Id != id)
            throw new DomainException(ErrorCodes.ProductNotFound, "The product was not found.");
        ReceivedDetails = details;
        ExistingProduct.Update(details);
        UpdatedProduct = ExistingProduct;
        return Task.FromResult(new ProductDto(id, details.Name, details.Description, details.Sku, details.Price, details.CategoryId, ExistingProduct.IsActive, 0));
    }

    public Task DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        if (ExistingProduct?.Id != id)
            throw new DomainException("PRODUCT_NOT_FOUND", "The product was not found.");
        ExistingProduct.Deactivate();
        return Task.CompletedTask;
    }
}
