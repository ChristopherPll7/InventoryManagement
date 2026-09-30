using InventoryManagement.Application.Abstractions;
using InventoryManagement.Domain.Common;

namespace InventoryManagement.Application.Inventory;

public sealed class GetProductInventoryMovementsQueryHandler(IInventoryQueries queries)
    : IQueryHandler<GetProductInventoryMovementsQuery, Result<PagedResult<InventoryMovementDto>>>
{
    public Task<Result<PagedResult<InventoryMovementDto>>> HandleAsync(GetProductInventoryMovementsQuery query, CancellationToken cancellationToken) =>
        BusinessResult.ExecuteAsync<PagedResult<InventoryMovementDto>>(async () =>
        {
            var filter = new InventoryMovementFilter(query.Type, query.StartDate, query.EndDate, query.Page, query.PageSize);
            var history = await queries.GetMovementsAsync(query.ProductId, filter, cancellationToken);
            return history is null
                ? Result<PagedResult<InventoryMovementDto>>.Failure(new Error(ErrorCodes.ProductNotFound, "The product was not found."))
                : Result<PagedResult<InventoryMovementDto>>.Success(history);
        });
}
