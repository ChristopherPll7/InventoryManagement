using InventoryManagement.Application.Abstractions;
using InventoryManagement.Domain.Common;

namespace InventoryManagement.Application.Inventory;

public sealed class GetProductInventoryMovementsQueryHandler(IInventoryQueries queries)
    : IQueryHandler<GetProductInventoryMovementsQuery, PagedResult<InventoryMovementDto>>
{
    public async Task<PagedResult<InventoryMovementDto>> HandleAsync(GetProductInventoryMovementsQuery query, CancellationToken cancellationToken) =>
        await queries.GetMovementsAsync(query.ProductId, query.Filter, cancellationToken)
        ?? throw new DomainException("PRODUCT_NOT_FOUND", "The product was not found.");
}
