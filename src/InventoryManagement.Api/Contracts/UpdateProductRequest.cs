namespace InventoryManagement.Api.Contracts;

public sealed class UpdateProductRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string Sku { get; init; }
    public required decimal Price { get; init; }
    public required Guid CategoryId { get; init; }
}
