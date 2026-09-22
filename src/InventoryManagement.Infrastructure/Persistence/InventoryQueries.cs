using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.Inventory;
using InventoryManagement.Domain.Inventory;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Persistence;

public sealed class InventoryQueries(InventoryReadDbContext dbContext) : IInventoryQueries
{
    public Task<ProductInventoryDto?> GetProductInventoryAsync(Guid productId, CancellationToken cancellationToken) =>
        dbContext.Products.AsNoTracking().Where(product => product.Id == productId)
            .Select(product => new ProductInventoryDto(product.Id, product.Sku, product.Name, EF.Property<int>(product, "CurrentStock")))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<InventoryMovementDto>?> GetMovementsAsync(Guid productId, InventoryMovementFilter filter, CancellationToken cancellationToken)
    {
        if (!await dbContext.Products.AsNoTracking().AnyAsync(product => product.Id == productId, cancellationToken))
            return null;
        var movements = ApplyFilters(dbContext.InventoryMovements.AsNoTracking().Where(movement => movement.ProductId == productId), filter);
        var totalCount = await movements.LongCountAsync(cancellationToken);
        var items = await movements.OrderByDescending(movement => movement.CreatedAt).ThenByDescending(movement => movement.Id)
            .Skip(filter.Offset).Take(filter.PageSize)
            .Select(movement => new InventoryMovementDto(movement.Id, movement.ProductId, movement.Type, movement.Quantity, movement.Reason, movement.CreatedAt))
            .ToListAsync(cancellationToken);
        return new PagedResult<InventoryMovementDto>(items, filter.Page, filter.PageSize, totalCount);
    }

    private static IQueryable<InventoryMovement> ApplyFilters(IQueryable<InventoryMovement> movements, InventoryMovementFilter filter)
    {
        if (filter.Type.HasValue)
            movements = movements.Where(movement => movement.Type == filter.Type.Value);
        if (filter.StartDate.HasValue)
            movements = movements.Where(movement => movement.CreatedAt >= filter.StartDate.Value);
        if (filter.EndDate.HasValue)
            movements = movements.Where(movement => movement.CreatedAt <= filter.EndDate.Value);
        return movements;
    }
}
