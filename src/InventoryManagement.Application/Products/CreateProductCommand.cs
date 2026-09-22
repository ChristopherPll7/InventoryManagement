using InventoryManagement.Application.Abstractions;

namespace InventoryManagement.Application.Products;

public sealed record CreateProductCommand(string Name, string? Description, string Sku, decimal Price, Guid CategoryId) : ICommand<Guid>;
