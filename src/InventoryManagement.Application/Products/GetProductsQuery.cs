using InventoryManagement.Application.Abstractions;

namespace InventoryManagement.Application.Products;

public sealed record GetProductsQuery(int Page = 1, int PageSize = PageRequest.DefaultPageSize) : IQuery<Result<PagedResult<ProductDto>>>;
