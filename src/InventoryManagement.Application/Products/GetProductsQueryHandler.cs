using InventoryManagement.Application.Abstractions;

namespace InventoryManagement.Application.Products;

public sealed class GetProductsQueryHandler(IProductQueries queries) : IQueryHandler<GetProductsQuery, IReadOnlyCollection<ProductDto>>
{
    public Task<IReadOnlyCollection<ProductDto>> HandleAsync(GetProductsQuery query, CancellationToken cancellationToken) =>
        queries.GetAllAsync(cancellationToken);
}
