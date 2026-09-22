using InventoryManagement.Domain.Products;

namespace InventoryManagement.Application.Abstractions;

public interface IProductValidationQueries
{
    Task<bool?> GetCategoryActiveStateAsync(Guid categoryId, CancellationToken cancellationToken);
    Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken, Guid? excludingProductId = null);
    Task<Product?> GetProductAsync(Guid id, CancellationToken cancellationToken);
}
