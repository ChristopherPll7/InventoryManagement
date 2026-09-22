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
        CreatedProduct = product;
        return Task.CompletedTask;
    }

    public Task<Product?> GetProductAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(ExistingProduct?.Id == id ? ExistingProduct : null);

    public Task<ProductDto> UpdateAsync(Product product, CancellationToken cancellationToken)
    {
        UpdatedProduct = product;
        return Task.FromResult(new ProductDto(product.Id, product.Name, product.Description, product.Sku, product.Price, product.CategoryId, product.IsActive, 0));
    }

    public Task DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        if (ExistingProduct?.Id != id)
            throw new DomainException("PRODUCT_NOT_FOUND", "The product was not found.");
        ExistingProduct.Deactivate();
        return Task.CompletedTask;
    }
}
