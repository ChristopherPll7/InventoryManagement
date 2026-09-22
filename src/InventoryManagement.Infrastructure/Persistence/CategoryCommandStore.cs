using Dapper;
using InventoryManagement.Application.Abstractions;
using InventoryManagement.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Persistence;

public sealed class CategoryCommandStore(SqlConnectionFactory connectionFactory) : ICategoryCommandStore
{
    public Task CreateAsync(Category category, CancellationToken cancellationToken) =>
        CatalogTransaction.ExecuteAsync(connectionFactory, (db, transaction) =>
        {
            const string sql = "INSERT INTO Categories (Id, Name, Description, IsActive, CreatedAt) VALUES (@Id, @Name, @Description, @IsActive, @CreatedAt)";
            return db.Database.GetDbConnection().ExecuteAsync(new CommandDefinition(sql, category, transaction, cancellationToken: cancellationToken));
        }, cancellationToken);

    public Task<CategoryDto> UpdateAsync(Category category, CancellationToken cancellationToken) =>
        CatalogTransaction.ExecuteAsync(connectionFactory, async (db, transaction) =>
        {
            var current = await CatalogState.GetCategoryAsync(db, category.Id, cancellationToken);
            current.Update(category.Name, category.Description);
            const string sql = "UPDATE Categories SET Name = @Name, Description = @Description, UpdatedAt = @UpdatedAt WHERE Id = @Id";
            await db.Database.GetDbConnection().ExecuteAsync(new CommandDefinition(sql, current, transaction, cancellationToken: cancellationToken));
            return CategoryDto.FromCategory(current);
        }, cancellationToken);

    public Task DeactivateAsync(Guid id, CancellationToken cancellationToken) =>
        CatalogTransaction.ExecuteAsync(connectionFactory, async (db, transaction) =>
        {
            var category = await CatalogState.GetCategoryAsync(db, id, cancellationToken);
            var hasActiveProducts = await db.Products.AsNoTracking().AnyAsync(product => product.CategoryId == id && product.IsActive, cancellationToken);
            category.Deactivate(hasActiveProducts);
            const string sql = "UPDATE Categories SET IsActive = @IsActive, UpdatedAt = @UpdatedAt WHERE Id = @Id";
            return await db.Database.GetDbConnection().ExecuteAsync(new CommandDefinition(sql, category, transaction, cancellationToken: cancellationToken));
        }, cancellationToken);
}
