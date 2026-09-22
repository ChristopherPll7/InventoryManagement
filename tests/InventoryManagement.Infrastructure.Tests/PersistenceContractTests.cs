using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Inventory;
using InventoryManagement.Domain.Products;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;

namespace InventoryManagement.Infrastructure.Tests;

[Collection("SqlServer")]
public sealed class PersistenceContractTests(SqlServerFixture database)
{
    [SqlServerFact]
    public async Task ConcurrentDuplicateSkuWritesReturnDomainConflict()
    {
        var sku = Guid.NewGuid().ToString("N");
        var first = Product.Create("First", null, sku, 1, database.CategoryId);
        var second = Product.Create("Second", null, sku, 1, database.CategoryId);
        var outcomes = await Task.WhenAll(
            CaptureDomainErrorAsync(() => database.CreateProductStore().CreateAsync(first, default)),
            CaptureDomainErrorAsync(() => database.CreateProductStore().CreateAsync(second, default)));
        Assert.Single(outcomes, code => code is null);
        Assert.Single(outcomes, code => code == "DUPLICATE_PRODUCT_SKU");
        await using var db = database.CreateReadContext();
        Assert.Equal(1, await db.Products.CountAsync(product => product.Sku == sku.ToUpperInvariant()));
    }

    [SqlServerFact]
    public async Task MissingAndInactiveProductsAreDistinguishedWithoutMovementWrites()
    {
        var store = database.CreateInventoryStore();
        var missing = InventoryMovement.Create(Guid.NewGuid(), InventoryMovementType.Entry, 1, null);
        Assert.Equal("PRODUCT_NOT_FOUND", await CaptureDomainErrorAsync(() => store.RegisterAsync(missing, default)));
        var product = await database.CreateProductAsync();
        var entry = InventoryMovement.Create(product.Id, InventoryMovementType.Entry, 5, null);
        await store.RegisterAsync(entry, default);
        await database.ExecuteAsync("UPDATE Products SET IsActive = 0 WHERE Id = @Id", new { product.Id });
        var inactive = InventoryMovement.Create(product.Id, InventoryMovementType.Entry, 1, null);
        Assert.Equal("PRODUCT_INACTIVE", await CaptureDomainErrorAsync(() => store.RegisterAsync(inactive, default)));
        var inactiveExit = InventoryMovement.Create(product.Id, InventoryMovementType.Exit, 1, null);
        Assert.Equal("PRODUCT_INACTIVE", await CaptureDomainErrorAsync(() => store.RegisterAsync(inactiveExit, default)));
        await AssertInventoryAsync(product.Id, 5, entry.Id);
        await using var db = database.CreateReadContext();
        Assert.False(await db.InventoryMovements.AnyAsync(movement => movement.ProductId == missing.ProductId));
    }

    [SqlServerFact]
    public async Task CategoryReadDistinguishesMissingActiveAndInactive()
    {
        var id = Guid.NewGuid();
        await database.ExecuteAsync(
            "INSERT INTO Categories (Id, Name, IsActive, CreatedAt) VALUES (@Id, @Name, 0, SYSUTCDATETIME())",
            new { Id = id, Name = id.ToString() });
        await using var db = database.CreateReadContext();
        var queries = new ProductValidationQueries(db);
        Assert.Null(await queries.GetCategoryActiveStateAsync(Guid.NewGuid(), default));
        Assert.True(await queries.GetCategoryActiveStateAsync(database.CategoryId, default));
        Assert.False(await queries.GetCategoryActiveStateAsync(id, default));
    }

    [SqlServerFact]
    public async Task EntryAndExitUseTheSamePersistedBalance()
    {
        var product = await database.CreateProductAsync();
        var store = database.CreateInventoryStore();
        var entry = await store.RegisterAsync(InventoryMovement.Create(product.Id, InventoryMovementType.Entry, 5, null), default);
        var exit = await store.RegisterAsync(InventoryMovement.Create(product.Id, InventoryMovementType.Exit, 2, null), default);
        Assert.Equal(5, entry.CurrentStock);
        Assert.Equal(3, exit.CurrentStock);
        await using var db = database.CreateReadContext();
        var result = await new ProductQueries(db).GetByIdAsync(product.Id, default);
        Assert.Equal(3, result!.CurrentStock);
        Assert.Equal(2, await db.InventoryMovements.CountAsync(movement => movement.ProductId == product.Id));
    }

    [SqlServerFact]
    public async Task OverflowLeavesStockAndHistoryUnchanged()
    {
        var product = await database.CreateProductAsync();
        await database.ExecuteAsync("UPDATE Products SET CurrentStock = @Stock WHERE Id = @Id", new { product.Id, Stock = int.MaxValue });
        var movement = InventoryMovement.Create(product.Id, InventoryMovementType.Entry, 1, null);
        Assert.Equal("INVENTORY_STOCK_OVERFLOW", await CaptureDomainErrorAsync(() => database.CreateInventoryStore().RegisterAsync(movement, default)));
        await using var db = database.CreateReadContext();
        var result = await new ProductQueries(db).GetByIdAsync(product.Id, default);
        Assert.Equal(int.MaxValue, result!.CurrentStock);
        Assert.False(await db.InventoryMovements.AnyAsync(item => item.ProductId == product.Id));
    }

    [SqlServerFact]
    public async Task ConcurrentExitsCannotSpendTheSameStock()
    {
        var product = await database.CreateProductAsync();
        await database.CreateInventoryStore().RegisterAsync(InventoryMovement.Create(product.Id, InventoryMovementType.Entry, 5, null), default);
        await using var blocker = database.CreateReadContext();
        await using var transaction = await blocker.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await blocker.Products.FromSqlInterpolated($"SELECT * FROM Products WITH (XLOCK, HOLDLOCK) WHERE Id = {product.Id}").AsNoTracking().SingleAsync();
        var first = CaptureDomainErrorAsync(() => database.CreateInventoryStore().RegisterAsync(InventoryMovement.Create(product.Id, InventoryMovementType.Exit, 4, null), default));
        var second = CaptureDomainErrorAsync(() => database.CreateInventoryStore().RegisterAsync(InventoryMovement.Create(product.Id, InventoryMovementType.Exit, 4, null), default));
        try
        {
            await AssertTwoRequestsAreBlockedAsync();
        }
        finally
        {
            await transaction.RollbackAsync();
            await Task.WhenAll(first, second);
        }
        var outcomes = new[] { await first, await second };
        Assert.Single(outcomes, code => code is null);
        Assert.Single(outcomes, code => code == "INSUFFICIENT_STOCK");
        await using var db = database.CreateReadContext();
        Assert.Equal(1, (await new ProductQueries(db).GetByIdAsync(product.Id, default))!.CurrentStock);
        Assert.Equal(2, await db.InventoryMovements.CountAsync(movement => movement.ProductId == product.Id));
    }

    [SqlServerFact]
    public async Task MissingCategoryAtWriteTimeReturnsNotFound()
    {
        var product = Product.Create("Product", null, Guid.NewGuid().ToString(), 1, Guid.NewGuid());
        Assert.Equal("CATEGORY_NOT_FOUND", await CaptureDomainErrorAsync(() => database.CreateProductStore().CreateAsync(product, default)));
    }

    [SqlServerFact]
    public async Task InsufficientStockPreservesExistingHistoryAndBalance()
    {
        var product = await database.CreateProductAsync();
        var store = database.CreateInventoryStore();
        var entry = InventoryMovement.Create(product.Id, InventoryMovementType.Entry, 3, null);
        await store.RegisterAsync(entry, default);
        var exit = InventoryMovement.Create(product.Id, InventoryMovementType.Exit, 5, null);
        Assert.Equal("INSUFFICIENT_STOCK", await CaptureDomainErrorAsync(() => store.RegisterAsync(exit, default)));
        await AssertInventoryAsync(product.Id, 3, entry.Id);
    }

    [SqlServerFact]
    public async Task FailureAfterMovementInsertRollsBackAndAllowsRetry() => await AssertWriteFailureRollsBackAsync(false);

    [SqlServerFact]
    public async Task FailureAfterStockUpdateRollsBackAndAllowsRetry() => await AssertWriteFailureRollsBackAsync(true);

    [SqlServerFact]
    public async Task NonAbortingSqlErrorRollsBackBothWrites() => await AssertWriteFailureRollsBackAsync(true, false);

    [SqlServerFact]
    public async Task DatabaseConstraintRejectsNegativeStockEvenOutsideDomain()
    {
        var product = await database.CreateProductAsync();
        var error = await Assert.ThrowsAsync<SqlException>(() => database.ExecuteAsync(
            "UPDATE Products SET CurrentStock = -1 WHERE Id = @Id", new { product.Id }));
        Assert.Equal(547, error.Number);
        Assert.Contains("CK_Products_CurrentStock", error.Message);
        await AssertInventoryAsync(product.Id, 0);
    }

    [SqlServerFact]
    public async Task QueriesDoNotTrackEntitiesOrChangeInventory()
    {
        var product = await database.CreateProductAsync();
        await using var db = database.CreateReadContext();
        var queries = new ProductQueries(db);
        Assert.NotNull(await queries.GetByIdAsync(product.Id, default));
        Assert.Contains(await queries.GetAllAsync(default), item => item.Id == product.Id);
        var validation = new ProductValidationQueries(db);
        Assert.True(await validation.SkuExistsAsync(product.Sku, default));
        Assert.True(await validation.GetCategoryActiveStateAsync(product.CategoryId, default));
        Assert.Empty(db.ChangeTracker.Entries());
        await AssertInventoryAsync(product.Id, 0);
    }

    private async Task AssertWriteFailureRollsBackAsync(bool afterStockUpdate, bool abortTransaction = true)
    {
        var product = await database.CreateProductAsync();
        var store = database.CreateInventoryStore();
        var entry = InventoryMovement.Create(product.Id, InventoryMovementType.Entry, 5, null);
        await store.RegisterAsync(entry, default);
        await using var db = database.CreateReadContext();
        var updatedAt = await db.Products.Where(item => item.Id == product.Id).Select(item => item.UpdatedAt).SingleAsync();
        var exit = InventoryMovement.Create(product.Id, InventoryMovementType.Exit, 2, null);
        await using (await SqlWriteFailure.CreateAsync(database, product.Id, exit.Id, afterStockUpdate, abortTransaction))
        {
            var error = await Assert.ThrowsAsync<SqlException>(() => store.RegisterAsync(exit, default));
            Assert.Equal(abortTransaction ? 51001 : 50000, error.Number);
            Assert.Contains("Injected inventory write failure.", error.Message);
            await AssertInventoryAsync(product.Id, 5, entry.Id);
            Assert.Equal(updatedAt, await db.Products.Where(item => item.Id == product.Id).Select(item => item.UpdatedAt).SingleAsync());
        }
        await store.RegisterAsync(exit, default);
        await AssertInventoryAsync(product.Id, 3, entry.Id, exit.Id);
    }

    private async Task AssertInventoryAsync(Guid productId, int stock, params Guid[] movementIds)
    {
        await using var db = database.CreateReadContext();
        Assert.Equal(stock, (await new ProductQueries(db).GetByIdAsync(productId, default))!.CurrentStock);
        var movements = await db.InventoryMovements.AsNoTracking().Where(item => item.ProductId == productId).ToListAsync();
        Assert.Equal(movementIds.Order(), movements.Select(item => item.Id).Order());
        Assert.Equal(stock, movements.Sum(item => item.Type == InventoryMovementType.Entry ? item.Quantity : -item.Quantity));
    }

    private async Task AssertTwoRequestsAreBlockedAsync()
    {
        await using var db = database.CreateReadContext();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (true)
        {
            var blocked = await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM sys.dm_exec_requests WHERE database_id = DB_ID() AND wait_type LIKE 'LCK_M%'")
                .SingleAsync(timeout.Token);
            if (blocked >= 2)
                return;
            await Task.Delay(25, timeout.Token);
        }
    }

    private static async Task<string?> CaptureDomainErrorAsync(Func<Task> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (DomainException exception)
        {
            return exception.Code;
        }
    }
}
