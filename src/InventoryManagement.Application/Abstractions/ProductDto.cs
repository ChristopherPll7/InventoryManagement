namespace InventoryManagement.Application.Abstractions;

public sealed record ProductDto(Guid Id, string Name, string? Description, string Sku, decimal Price, Guid CategoryId, bool IsActive, int CurrentStock);
