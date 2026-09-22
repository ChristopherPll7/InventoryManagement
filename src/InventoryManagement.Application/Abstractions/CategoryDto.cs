using InventoryManagement.Domain.Categories;

namespace InventoryManagement.Application.Abstractions;

public sealed record CategoryDto(Guid Id, string Name, string? Description, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt)
{
    public static CategoryDto FromCategory(Category category) =>
        new(category.Id, category.Name, category.Description, category.IsActive, category.CreatedAt, category.UpdatedAt);
}
