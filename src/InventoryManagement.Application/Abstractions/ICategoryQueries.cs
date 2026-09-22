using InventoryManagement.Domain.Categories;

namespace InventoryManagement.Application.Abstractions;

public interface ICategoryQueries
{
    Task<Category?> GetEntityAsync(Guid id, CancellationToken cancellationToken);
    Task<CategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<CategoryDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken, Guid? excludingCategoryId = null);
}
