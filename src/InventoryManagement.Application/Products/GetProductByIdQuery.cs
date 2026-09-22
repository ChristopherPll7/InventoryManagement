using InventoryManagement.Application.Abstractions;

namespace InventoryManagement.Application.Products;

public sealed record GetProductByIdQuery(Guid Id) : IQuery<ProductDto?>;
