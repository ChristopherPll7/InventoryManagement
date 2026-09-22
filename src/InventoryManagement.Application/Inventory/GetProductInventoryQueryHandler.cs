using InventoryManagement.Application.Abstractions;
using InventoryManagement.Domain.Common;

namespace InventoryManagement.Application.Inventory;

public sealed class GetProductInventoryQueryHandler(IInventoryQueries queries) : IQueryHandler<GetProductInventoryQuery, ProductInventoryDto>
{
    public async Task<ProductInventoryDto> HandleAsync(GetProductInventoryQuery query, CancellationToken cancellationToken) =>
        await queries.GetProductInventoryAsync(query.ProductId, cancellationToken)
        ?? throw new DomainException("PRODUCT_NOT_FOUND", "The product was not found.");
}
