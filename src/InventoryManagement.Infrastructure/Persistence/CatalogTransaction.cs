using System.Data;
using System.Data.Common;
using InventoryManagement.Domain.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Persistence;

internal static class CatalogTransaction
{
    public static async Task<T> ExecuteAsync<T>(SqlConnectionFactory factory, Func<InventoryReadDbContext, DbTransaction, Task<T>> action, CancellationToken cancellationToken)
    {
        await using var connection = factory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await using var db = new InventoryReadDbContext(new DbContextOptionsBuilder<InventoryReadDbContext>().UseSqlServer(connection).Options);
        await db.Database.UseTransactionAsync(transaction, cancellationToken);
        try
        {
            var result = await action(db, transaction);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            if (exception is SqlException sqlException && MapConflict(sqlException) is { } conflict)
                throw conflict;
            throw;
        }
    }

    private static DomainException? MapConflict(SqlException exception)
    {
        if (exception.Number is 2601 or 2627)
        {
            if (exception.Message.Contains("UQ_Products_Sku", StringComparison.Ordinal))
                return new DomainException("DUPLICATE_PRODUCT_SKU", "The SKU is already in use.");
            if (exception.Message.Contains("UQ_Categories_Name", StringComparison.Ordinal))
                return new DomainException("DUPLICATE_CATEGORY_NAME", "The category name is already in use.");
        }
        return null;
    }
}
