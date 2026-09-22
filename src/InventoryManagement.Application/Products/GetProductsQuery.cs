using InventoryManagement.Application.Abstractions;

namespace InventoryManagement.Application.Products;

public sealed record GetProductsQuery : IQuery<IReadOnlyCollection<ProductDto>>;
