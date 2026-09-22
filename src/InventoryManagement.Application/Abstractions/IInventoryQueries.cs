using InventoryManagement.Application.Inventory;

namespace InventoryManagement.Application.Abstractions;

public interface IInventoryQueries
{
    Task<ProductInventoryDto?> GetProductInventoryAsync(Guid productId, CancellationToken cancellationToken);
    Task<PagedResult<InventoryMovementDto>?> GetMovementsAsync(Guid productId, InventoryMovementFilter filter, CancellationToken cancellationToken);
}
