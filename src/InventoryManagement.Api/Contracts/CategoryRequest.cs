namespace InventoryManagement.Api.Contracts;

public sealed class CategoryRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
}
