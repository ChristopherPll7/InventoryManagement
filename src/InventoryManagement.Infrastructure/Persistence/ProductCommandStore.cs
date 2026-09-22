using Dapper;
using InventoryManagement.Application.Abstractions;
using InventoryManagement.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Persistence;

public sealed class ProductCommandStore(SqlConnectionFactory connectionFactory) : IProductCommandStore
{
    public Task CreateAsync(Product product, CancellationToken cancellationToken) =>
        CatalogTransaction.ExecuteAsync(connectionFactory, async (db, transaction) =>
        {
            await CatalogState.EnsureCategoryActiveAsync(db, product.CategoryId, cancellationToken);
            const string sql = """
                INSERT INTO Products (Id, Name, Description, Sku, Price, CategoryId, IsActive, CurrentStock, CreatedAt)
                VALUES (@Id, @Name, @Description, @Sku, @Price, @CategoryId, @IsActive, 0, @CreatedAt)
                """;
            return await db.Database.GetDbConnection().ExecuteAsync(new CommandDefinition(sql, product, transaction, cancellationToken: cancellationToken));
        }, cancellationToken);

    public Task<ProductDto> UpdateAsync(Product product, CancellationToken cancellationToken) =>
        CatalogTransaction.ExecuteAsync(connectionFactory, async (db, transaction) =>
        {
            await CatalogState.EnsureCategoryActiveAsync(db, product.CategoryId, cancellationToken);
            var current = await CatalogState.GetProductAsync(db, product.Id, cancellationToken);
            current.Update(product.Name, product.Description, product.Sku, product.Price, product.CategoryId);
            const string sql = """
                UPDATE Products SET Name = @Name, Description = @Description, Sku = @Sku,
                    Price = @Price, CategoryId = @CategoryId, UpdatedAt = @UpdatedAt WHERE Id = @Id
                """;
            await db.Database.GetDbConnection().ExecuteAsync(new CommandDefinition(sql, current, transaction, cancellationToken: cancellationToken));
            return (await new ProductQueries(db).GetByIdAsync(current.Id, cancellationToken))!;
        }, cancellationToken);

    public Task DeactivateAsync(Guid id, CancellationToken cancellationToken) =>
        CatalogTransaction.ExecuteAsync(connectionFactory, async (db, transaction) =>
        {
            var product = await CatalogState.GetProductAsync(db, id, cancellationToken);
            product.Deactivate();
            const string sql = "UPDATE Products SET IsActive = @IsActive, UpdatedAt = @UpdatedAt WHERE Id = @Id";
            return await db.Database.GetDbConnection().ExecuteAsync(new CommandDefinition(sql, product, transaction, cancellationToken: cancellationToken));
        }, cancellationToken);
}
