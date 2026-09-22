using InventoryManagement.Application.Abstractions;

namespace InventoryManagement.Application.Inventory;

public sealed record GetProductInventoryQuery(Guid ProductId) : IQuery<ProductInventoryDto>;
