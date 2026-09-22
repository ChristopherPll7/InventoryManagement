namespace InventoryManagement.Application.Abstractions;

public sealed record ProductInventoryDto(Guid ProductId, string Sku, string Name, int CurrentStock);
