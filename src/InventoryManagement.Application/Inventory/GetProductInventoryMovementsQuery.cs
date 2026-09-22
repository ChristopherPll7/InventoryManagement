using InventoryManagement.Application.Abstractions;

namespace InventoryManagement.Application.Inventory;

public sealed record GetProductInventoryMovementsQuery(Guid ProductId, InventoryMovementFilter Filter) : IQuery<PagedResult<InventoryMovementDto>>;
