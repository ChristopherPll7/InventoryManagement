using InventoryManagement.Domain.Categories;
using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Persistence;

internal static class CatalogState
{
    public static async Task<Category> GetCategoryAsync(InventoryReadDbContext db, Guid id, CancellationToken cancellationToken) =>
        await db.Categories.FromSqlInterpolated($"SELECT * FROM Categories WITH (UPDLOCK, HOLDLOCK) WHERE Id = {id}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken)
        ?? throw new DomainException("CATEGORY_NOT_FOUND", "The category was not found.");

    public static async Task<Product> GetProductAsync(InventoryReadDbContext db, Guid id, CancellationToken cancellationToken) =>
        await db.Products.FromSqlInterpolated($"SELECT * FROM Products WITH (UPDLOCK, HOLDLOCK) WHERE Id = {id}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken)
        ?? throw new DomainException("PRODUCT_NOT_FOUND", "The product was not found.");

    public static async Task EnsureCategoryActiveAsync(InventoryReadDbContext db, Guid id, CancellationToken cancellationToken)
    {
        var category = await GetCategoryAsync(db, id, cancellationToken);
        category.EnsureActive();
    }
}
