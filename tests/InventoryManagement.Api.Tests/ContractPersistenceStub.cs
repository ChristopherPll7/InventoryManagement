using InventoryManagement.Application.Abstractions;
using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Inventory;
using InventoryManagement.Domain.Products;

namespace InventoryManagement.Api.Tests;

public sealed class ContractPersistenceStub : IProductCommandStore, IProductValidationQueries, IProductQueries, IInventoryCommandStore
{
    public bool? CategoryIsActive { get; set; } = true;
    public bool DuplicateSku { get; set; }
    public string? InventoryError { get; set; }
    public string? ProductWriteError { get; set; }
    public int WriteCount { get; private set; }
    public Product? Product { get; private set; }

    public Task<bool?> GetCategoryActiveStateAsync(Guid categoryId, CancellationToken cancellationToken) => Task.FromResult(CategoryIsActive);
    public Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken, Guid? excludingProductId = null) => Task.FromResult(DuplicateSku);
    public Task<Product?> GetProductAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Product?.Id == id ? Product : null);

    public Task CreateAsync(Product product, CancellationToken cancellationToken)
    {
        if (ProductWriteError is not null)
            throw new DomainException(ProductWriteError, "Product write rejected.");
        WriteCount++;
        Product = product;
        return Task.CompletedTask;
    }

    public Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Product?.Id == id ? MapProduct(Product) : null);
    public Task<IReadOnlyCollection<ProductDto>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<ProductDto>>(Product is null ? [] : [MapProduct(Product)]);

    public Task<ProductDto> UpdateAsync(Product product, CancellationToken cancellationToken)
    {
        if (ProductWriteError is not null)
            throw new DomainException(ProductWriteError, "Product write rejected.");
        Product = product;
        WriteCount++;
        return Task.FromResult(MapProduct(product));
    }

    public Task DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        if (Product?.Id != id)
            throw new DomainException("PRODUCT_NOT_FOUND", "The product was not found.");
        Product.Deactivate();
        WriteCount++;
        return Task.CompletedTask;
    }

    private static ProductDto MapProduct(Product product) =>
        new(product.Id, product.Name, product.Description, product.Sku, product.Price, product.CategoryId, product.IsActive, 10);

    public Task<InventoryMovementResult> RegisterAsync(InventoryMovement movement, CancellationToken cancellationToken)
    {
        if (InventoryError is not null)
            throw new DomainException(InventoryError, "Inventory operation rejected.");
        var stock = movement.CalculateResultingStock(10);
        WriteCount++;
        return Task.FromResult(new InventoryMovementResult(movement.Id, stock));
    }
}
