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

    public async Task<IReadOnlyCollection<ProductDto>> GetAllAsync(CancellationToken cancellationToken) => await dbContext.Products
        .AsNoTracking()
        .OrderBy(product => product.Name)
        .Select(product => new ProductDto(product.Id, product.Name, product.Description, product.Sku, product.Price, product.CategoryId, product.IsActive, EF.Property<int>(product, "CurrentStock")))
        .ToListAsync(cancellationToken);
}
