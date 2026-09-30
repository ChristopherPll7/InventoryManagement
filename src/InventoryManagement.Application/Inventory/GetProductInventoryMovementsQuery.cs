using InventoryManagement.Application.Abstractions;
using InventoryManagement.Domain.Inventory;

namespace InventoryManagement.Application.Inventory;

public sealed record GetProductInventoryMovementsQuery(
    Guid ProductId,
    InventoryMovementType? Type = null,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? EndDate = null,
    int Page = 1,
    int PageSize = PageRequest.DefaultPageSize) : IQuery<Result<PagedResult<InventoryMovementDto>>>;
