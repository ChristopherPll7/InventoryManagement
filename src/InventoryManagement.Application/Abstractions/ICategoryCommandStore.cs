using InventoryManagement.Domain.Categories;

namespace InventoryManagement.Application.Abstractions;

public interface ICategoryCommandStore
{
    Task CreateAsync(Category category, CancellationToken cancellationToken);
    Task<CategoryDto> UpdateAsync(Category category, CancellationToken cancellationToken);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken);
}
