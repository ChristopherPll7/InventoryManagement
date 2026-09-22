using System.Data;
using Dapper;
using InventoryManagement.Application.Abstractions;
using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace InventoryManagement.Infrastructure.Persistence;

public sealed class InventoryCommandStore(SqlConnectionFactory connectionFactory) : IInventoryCommandStore
{
    public async Task<InventoryMovementResult> RegisterAsync(InventoryMovement movement, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var currentStock = await GetLockedStockAsync(connection, transaction, movement.ProductId, cancellationToken);
            var resultingStock = movement.CalculateResultingStock(currentStock);
            await InsertMovementAsync(connection, transaction, movement, cancellationToken);
            await UpdateStockAsync(connection, transaction, movement.ProductId, resultingStock, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new InventoryMovementResult(movement.Id, resultingStock);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static async Task<int> GetLockedStockAsync(DbConnection connection, DbTransaction transaction, Guid productId, CancellationToken cancellationToken)
    {
        var options = new DbContextOptionsBuilder<InventoryReadDbContext>().UseSqlServer(connection).Options;
        await using var dbContext = new InventoryReadDbContext(options);
        await dbContext.Database.UseTransactionAsync(transaction, cancellationToken);
        var state = await dbContext.Products
            .FromSqlInterpolated($"SELECT * FROM Products WITH (UPDLOCK, HOLDLOCK) WHERE Id = {productId}")
            .AsNoTracking()
            .Select(product => new { Product = product, CurrentStock = EF.Property<int>(product, "CurrentStock") })
            .SingleOrDefaultAsync(cancellationToken);
        if (state is null)
            throw new DomainException("PRODUCT_NOT_FOUND", "The product was not found.");
        state.Product.EnsureActive();
        return state.CurrentStock;
    }

    private static Task<int> InsertMovementAsync(IDbConnection connection, IDbTransaction transaction, InventoryMovement movement, CancellationToken cancellationToken)
    {
        const string sql = "INSERT INTO InventoryMovements (Id, ProductId, Type, Quantity, Reason, CreatedAt) VALUES (@Id, @ProductId, @Type, @Quantity, @Reason, @CreatedAt)";
        return connection.ExecuteAsync(new CommandDefinition(sql, movement, transaction, cancellationToken: cancellationToken));
    }

    private static Task<int> UpdateStockAsync(IDbConnection connection, IDbTransaction transaction, Guid productId, int stock, CancellationToken cancellationToken)
    {
        const string sql = "UPDATE Products SET CurrentStock = @Stock, UpdatedAt = SYSUTCDATETIME() WHERE Id = @ProductId";
        return connection.ExecuteAsync(new CommandDefinition(sql, new { ProductId = productId, Stock = stock }, transaction, cancellationToken: cancellationToken));
    }
}
