using InventoryManagement.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using InventoryManagement.Domain.Products;

namespace InventoryManagement.Infrastructure.Persistence;

public sealed class ProductValidationQueries(InventoryReadDbContext dbContext) : IProductValidationQueries
{
    public Task<bool?> GetCategoryActiveStateAsync(Guid categoryId, CancellationToken cancellationToken) =>
        dbContext.Categories.AsNoTracking().Where(category => category.Id == categoryId)
            .Select(category => (bool?)category.IsActive).SingleOrDefaultAsync(cancellationToken);

    public Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken, Guid? excludingProductId = null) =>
        dbContext.Products.AsNoTracking().AnyAsync(product => product.Sku == sku && product.Id != excludingProductId, cancellationToken);

    public Task<Product?> GetProductAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Products.AsNoTracking().SingleOrDefaultAsync(product => product.Id == id, cancellationToken);
}
