namespace InventoryManagement.Infrastructure.Tests;

internal sealed class SqlWriteFailure(SqlServerFixture database, string triggerName) : IAsyncDisposable
{
    public static async Task<SqlWriteFailure> CreateAsync(SqlServerFixture database, Guid productId, Guid movementId, bool afterStockUpdate, bool abortTransaction)
    {
        var triggerName = $"InventoryFault_{Guid.NewGuid():N}";
        var table = afterStockUpdate ? "Products" : "InventoryMovements";
        var operation = afterStockUpdate ? "UPDATE" : "INSERT";
        var key = afterStockUpdate ? "Id" : "ProductId";
        var xactAbort = abortTransaction ? "ON" : "OFF";
        var errorStatement = abortTransaction
            ? "THROW 51001, 'Injected inventory write failure.', 1;"
            : "RAISERROR ('Injected inventory write failure.', 16, 1); RETURN;";
        var sql = $"""
            CREATE TRIGGER [{triggerName}] ON dbo.[{table}] AFTER {operation} AS
            BEGIN
                SET NOCOUNT ON;
                SET XACT_ABORT {xactAbort};
                IF EXISTS (SELECT 1 FROM inserted WHERE [{key}] = '{productId}')
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM dbo.InventoryMovements WHERE Id = '{movementId}')
                        THROW 51002, 'The movement must exist before the injected failure.', 1;
                    {errorStatement}
                END;
            END;
            """;
        await database.ExecuteAsync(sql, new { });
        return new SqlWriteFailure(database, triggerName);
    }

    public async ValueTask DisposeAsync() =>
        await database.ExecuteAsync($"DROP TRIGGER [{triggerName}]", new { });
}
