using InventoryManagement.Application.Abstractions;
using InventoryManagement.Domain.Categories;
using InventoryManagement.Domain.Common;

namespace InventoryManagement.Application.Tests;

public sealed class CategoryPersistenceStub : ICategoryCommandStore, ICategoryQueries
{
    public Category? Category { get; private set; }
    public bool DuplicateName { get; set; }
    public bool HasActiveProducts { get; set; }
    public string? WriteError { get; set; }
    public Guid? ExcludedCategoryId { get; private set; }
    public int WriteCount { get; private set; }

    public Task CreateAsync(Category category, CancellationToken cancellationToken)
    {
        if (WriteError is not null)
            throw new DomainException(WriteError, "Category write rejected.");
        Category = category;
        WriteCount++;
        return Task.CompletedTask;
    }

    public Task<CategoryDto> UpdateAsync(Category category, CancellationToken cancellationToken)
    {
        if (WriteError is not null)
            throw new DomainException(WriteError, "Category write rejected.");
        Category = category;
        WriteCount++;
        return Task.FromResult(CategoryDto.FromCategory(category));
    }

    public Task DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        if (Category?.Id != id)
            throw new DomainException("CATEGORY_NOT_FOUND", "The category was not found.");
        Category.Deactivate(HasActiveProducts);
        WriteCount++;
        return Task.CompletedTask;
    }

    public Task<Category?> GetEntityAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Category?.Id == id ? Category : null);

    public Task<CategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Category?.Id == id ? CategoryDto.FromCategory(Category) : null);

    public Task<PagedResult<CategoryDto>> GetAllAsync(PageRequest pagination, CancellationToken cancellationToken) =>
        Task.FromResult(new PagedResult<CategoryDto>(
            Category is null || pagination.Page > 1 ? [] : [CategoryDto.FromCategory(Category)],
            pagination.Page, pagination.PageSize, Category is null ? 0 : 1));

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken, Guid? excludingCategoryId = null)
    {
        ExcludedCategoryId = excludingCategoryId;
        return Task.FromResult(DuplicateName);
    }
}
