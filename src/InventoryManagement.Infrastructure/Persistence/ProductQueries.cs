using InventoryManagement.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Persistence;

public sealed class ProductQueries(InventoryReadDbContext dbContext) : IProductQueries
{
    public Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => dbContext.Products
        .AsNoTracking()
        .Where(product => product.Id == id)
        .Select(product => new ProductDto(product.Id, product.Name, product.Description, product.Sku, product.Price, product.CategoryId, product.IsActive, EF.Property<int>(product, "CurrentStock")))
        .SingleOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<ProductDto>> GetAllAsync(PageRequest pagination, CancellationToken cancellationToken)
    {
        var products = dbContext.Products.AsNoTracking();
        var totalCount = await products.LongCountAsync(cancellationToken);
        var items = await products.OrderBy(product => product.Name).ThenBy(product => product.Id)
            .Skip(pagination.Offset).Take(pagination.PageSize)
            .Select(product => new ProductDto(product.Id, product.Name, product.Description, product.Sku, product.Price, product.CategoryId, product.IsActive, EF.Property<int>(product, "CurrentStock")))
            .ToListAsync(cancellationToken);
        return new PagedResult<ProductDto>(items, pagination.Page, pagination.PageSize, totalCount);
    }
}
