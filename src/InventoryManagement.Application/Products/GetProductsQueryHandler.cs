using InventoryManagement.Application.Abstractions;

namespace InventoryManagement.Application.Products;

public sealed class GetProductsQueryHandler(IProductQueries queries) : IQueryHandler<GetProductsQuery, Result<PagedResult<ProductDto>>>
{
    public Task<Result<PagedResult<ProductDto>>> HandleAsync(GetProductsQuery query, CancellationToken cancellationToken) =>
        BusinessResult.ExecuteAsync<PagedResult<ProductDto>>(async () =>
        {
            return Result<PagedResult<ProductDto>>.Success(await queries.GetAllAsync(new PageRequest(query.Page, query.PageSize), cancellationToken));
        });
}
