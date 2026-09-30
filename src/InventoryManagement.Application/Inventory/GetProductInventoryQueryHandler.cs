using InventoryManagement.Domain.Common;
using InventoryManagement.Application.Abstractions;

namespace InventoryManagement.Application.Inventory;

public sealed class GetProductInventoryQueryHandler(IInventoryQueries queries) : IQueryHandler<GetProductInventoryQuery, Result<ProductInventoryDto>>
{
    public async Task<Result<ProductInventoryDto>> HandleAsync(GetProductInventoryQuery query, CancellationToken cancellationToken)
    {
        var product = await queries.GetProductInventoryAsync(query.ProductId, cancellationToken);
        return product is null
            ? Result<ProductInventoryDto>.Failure(new Error(ErrorCodes.ProductNotFound, "The product was not found."))
            : Result<ProductInventoryDto>.Success(product);
    }
}
