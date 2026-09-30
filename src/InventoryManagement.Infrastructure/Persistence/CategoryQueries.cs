using InventoryManagement.Application.Abstractions;
using InventoryManagement.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Persistence;

public sealed class CategoryQueries(InventoryReadDbContext dbContext) : ICategoryQueries
{
    public Task<Category?> GetEntityAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Categories.AsNoTracking().SingleOrDefaultAsync(category => category.Id == id, cancellationToken);

    public Task<CategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        ProjectCategories(dbContext.Categories.Where(category => category.Id == id)).SingleOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<CategoryDto>> GetAllAsync(PageRequest pagination, CancellationToken cancellationToken)
    {
        var categories = dbContext.Categories.AsNoTracking();
        var totalCount = await categories.LongCountAsync(cancellationToken);
        var items = await ProjectCategories(categories.OrderBy(category => category.Name).ThenBy(category => category.Id)
            .Skip(pagination.Offset).Take(pagination.PageSize)).ToListAsync(cancellationToken);
        return new PagedResult<CategoryDto>(items, pagination.Page, pagination.PageSize, totalCount);
    }

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken, Guid? excludingCategoryId = null) =>
        dbContext.Categories.AsNoTracking().AnyAsync(category => category.Name == name && category.Id != excludingCategoryId, cancellationToken);

    private static IQueryable<CategoryDto> ProjectCategories(IQueryable<Category> categories) => categories.AsNoTracking()
        .Select(category => new CategoryDto(category.Id, category.Name, category.Description, category.IsActive, category.CreatedAt, category.UpdatedAt));
}
