using InventoryManagement.Application.Abstractions;

namespace InventoryManagement.Application.Products;

public sealed class GetProductByIdQueryHandler(IProductQueries queries) : IQueryHandler<GetProductByIdQuery, ProductDto?>
{
    public Task<ProductDto?> HandleAsync(GetProductByIdQuery query, CancellationToken cancellationToken) =>
        queries.GetByIdAsync(query.Id, cancellationToken);
}
