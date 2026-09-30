using InventoryManagement.Domain.Common;
using InventoryManagement.Application.Abstractions;

namespace InventoryManagement.Application.Products;

public sealed class GetProductByIdQueryHandler(IProductQueries queries) : IQueryHandler<GetProductByIdQuery, Result<ProductDto>>
{
    public Task<Result<ProductDto>> HandleAsync(GetProductByIdQuery query, CancellationToken cancellationToken) =>
        BusinessResult.ExecuteAsync<ProductDto>(async () =>
        {
            var item = await queries.GetByIdAsync(query.Id, cancellationToken);
            return item is null
                ? Result<ProductDto>.Failure(new Error(ErrorCodes.ProductNotFound, "The product was not found."))
                : Result<ProductDto>.Success(item);
        });
}
