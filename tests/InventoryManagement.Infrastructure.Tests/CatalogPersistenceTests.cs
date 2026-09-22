using System.Data;
using InventoryManagement.Domain.Categories;
using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Inventory;
using InventoryManagement.Domain.Products;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Tests;

[Collection("SqlServer")]
public sealed class CatalogPersistenceTests(SqlServerFixture database)
{
    private CategoryCommandStore CategoryStore => new(new SqlConnectionFactory(database.ConnectionString));

    [SqlServerFact]
    public async Task CategoryCrudPreservesIdentityAndInactiveRecordsRemainReadable()
    {
        var category = await CreateCategoryAsync();
        var createdAt = category.CreatedAt;
        await using var db = database.CreateReadContext();
        var queries = new CategoryQueries(db);
        Assert.Equal(category.Name, (await queries.GetByIdAsync(category.Id, default))!.Name);
        Assert.False(await queries.NameExistsAsync(category.Name, default, category.Id));
        category.Update("Updated-" + category.Id, "Details");
        var updated = await CategoryStore.UpdateAsync(category, default);
        Assert.Equal(createdAt, updated.CreatedAt);
        Assert.Equal(category.Id, updated.Id);
        Assert.Equal("Details", updated.Description);
        await CategoryStore.DeactivateAsync(category.Id, default);
        var inactive = (await queries.GetByIdAsync(category.Id, default))!;
        await CategoryStore.DeactivateAsync(category.Id, default);
        Assert.Equal(inactive.UpdatedAt, (await queries.GetByIdAsync(category.Id, default))!.UpdatedAt);
        Assert.False(inactive.IsActive);
        Assert.Contains(await queries.GetAllAsync(default), item => item.Id == category.Id && !item.IsActive);
        category.Update("Renamed-" + category.Id, null);
        Assert.False((await CategoryStore.UpdateAsync(category, default)).IsActive);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [SqlServerFact]
    public async Task ConcurrentDuplicateCategoryCreatesYieldOneConflict()
    {
        var name = Guid.NewGuid().ToString();
        var outcomes = await Task.WhenAll(
            CaptureErrorAsync(() => CategoryStore.CreateAsync(Category.Create(name, null), default)),
            CaptureErrorAsync(() => CategoryStore.CreateAsync(Category.Create(name, null), default)));
        Assert.Single(outcomes, code => code is null);
        Assert.Single(outcomes, code => code == "DUPLICATE_CATEGORY_NAME");
        await using var db = database.CreateReadContext();
        Assert.Equal(1, await db.Categories.CountAsync(category => category.Name == name));
    }

    [SqlServerFact]
    public async Task DuplicateCategoryRenameRollsBackAndInactiveNamesRemainReserved()
    {
        var first = await CreateCategoryAsync();
        var second = await CreateCategoryAsync();
        await CategoryStore.DeactivateAsync(first.Id, default);
        var originalName = second.Name;
        second.Update(first.Name, "Must not persist");
        Assert.Equal("DUPLICATE_CATEGORY_NAME", await CaptureErrorAsync(() => CategoryStore.UpdateAsync(second, default)));
        Assert.Equal("DUPLICATE_CATEGORY_NAME", await CaptureErrorAsync(() => CategoryStore.CreateAsync(Category.Create(first.Name, null), default)));
        await using var db = database.CreateReadContext();
        var stored = (await new CategoryQueries(db).GetByIdAsync(second.Id, default))!;
        Assert.Equal(originalName, stored.Name);
        Assert.Null(stored.Description);
        Assert.Null(stored.UpdatedAt);
    }

    [SqlServerFact]
    public async Task CategoryDeleteBlocksActiveProductsAndPreservesInactiveProductHistory()
    {
        var category = await CreateCategoryAsync();
        var product = await CreateProductAsync(category.Id);
        var entry = InventoryMovement.Create(product.Id, InventoryMovementType.Entry, 5, null);
        await database.CreateInventoryStore().RegisterAsync(entry, default);
        Assert.Equal("CATEGORY_IN_USE", await CaptureErrorAsync(() => CategoryStore.DeactivateAsync(category.Id, default)));
        await database.CreateProductStore().DeactivateAsync(product.Id, default);
        await CategoryStore.DeactivateAsync(category.Id, default);
        await using var db = database.CreateReadContext();
        Assert.False((await new CategoryQueries(db).GetByIdAsync(category.Id, default))!.IsActive);
        var stored = (await new ProductQueries(db).GetByIdAsync(product.Id, default))!;
        Assert.False(stored.IsActive);
        Assert.Equal(category.Id, stored.CategoryId);
        Assert.Equal(5, stored.CurrentStock);
        Assert.True(await db.InventoryMovements.AnyAsync(movement => movement.Id == entry.Id));
        Assert.Equal("PRODUCT_INACTIVE", await CaptureErrorAsync(() =>
            database.CreateInventoryStore().RegisterAsync(InventoryMovement.Create(product.Id, InventoryMovementType.Exit, 1, null), default)));
    }

    [SqlServerFact]
    public async Task InactiveCategoryCannotReceiveProductCreatesOrUpdates()
    {
        var category = await CreateCategoryAsync();
        await CategoryStore.DeactivateAsync(category.Id, default);
        var candidate = Product.Create("Product", null, Guid.NewGuid().ToString(), 1, category.Id);
        Assert.Equal("CATEGORY_INACTIVE", await CaptureErrorAsync(() => database.CreateProductStore().CreateAsync(candidate, default)));
        var existing = await database.CreateProductAsync();
        existing.Update("Changed", null, existing.Sku, 2, category.Id);
        Assert.Equal("CATEGORY_INACTIVE", await CaptureErrorAsync(() => database.CreateProductStore().UpdateAsync(existing, default)));
        await using var db = database.CreateReadContext();
        Assert.False(await db.Products.AnyAsync(product => product.Id == candidate.Id));
        var stored = (await new ProductQueries(db).GetByIdAsync(existing.Id, default))!;
        Assert.Equal(database.CategoryId, stored.CategoryId);
        Assert.Equal(1, stored.Price);
    }

    [SqlServerFact]
    public async Task ProductUpdateChangesDetailsWithoutChangingStockHistoryOrCreationDate()
    {
        var product = await database.CreateProductAsync();
        var category = await CreateCategoryAsync();
        var entry = InventoryMovement.Create(product.Id, InventoryMovementType.Entry, 7, null);
        await database.CreateInventoryStore().RegisterAsync(entry, default);
        product.Update(" Updated ", "Description", " new-" + product.Id, 12.50m, category.Id);
        var result = await database.CreateProductStore().UpdateAsync(product, default);
        Assert.Equal(product.Id, result.Id);
        Assert.Equal("Updated", result.Name);
        Assert.Equal(product.Sku, result.Sku);
        Assert.Equal(category.Id, result.CategoryId);
        Assert.Equal(7, result.CurrentStock);
        await using var db = database.CreateReadContext();
        Assert.Equal(product.CreatedAt, await db.Products.Where(item => item.Id == product.Id).Select(item => item.CreatedAt).SingleAsync());
        Assert.Equal(entry.Id, (await db.InventoryMovements.SingleAsync(item => item.ProductId == product.Id)).Id);
        Assert.False(await new ProductValidationQueries(db).SkuExistsAsync(result.Sku, default, result.Id));
    }

    [SqlServerFact]
    public async Task ProductDuplicateUpdateRollsBackAndInactiveSkusRemainReserved()
    {
        var first = await database.CreateProductAsync();
        var second = await database.CreateProductAsync();
        await database.CreateProductStore().DeactivateAsync(first.Id, default);
        var originalSku = second.Sku;
        second.Update("Must not persist", null, first.Sku, 99, second.CategoryId);
        Assert.Equal("DUPLICATE_PRODUCT_SKU", await CaptureErrorAsync(() => database.CreateProductStore().UpdateAsync(second, default)));
        await using var db = database.CreateReadContext();
        var stored = (await new ProductQueries(db).GetByIdAsync(second.Id, default))!;
        Assert.Equal(originalSku, stored.Sku);
        Assert.Equal(1, stored.Price);
        Assert.NotEqual("Must not persist", stored.Name);
    }

    [SqlServerFact]
    public async Task RepeatedProductDeletionPreservesTimestampAndStaleUpdateCannotReactivate()
    {
        var product = await database.CreateProductAsync();
        await database.CreateProductStore().DeactivateAsync(product.Id, default);
        await using var db = database.CreateReadContext();
        var timestamp = await db.Products.Where(item => item.Id == product.Id).Select(item => item.UpdatedAt).SingleAsync();
        await database.CreateProductStore().DeactivateAsync(product.Id, default);
        Assert.Equal(timestamp, await db.Products.Where(item => item.Id == product.Id).Select(item => item.UpdatedAt).SingleAsync());
        Assert.True(product.IsActive);
        product.Update("Changed after deletion", null, product.Sku, 2, product.CategoryId);
        Assert.False((await database.CreateProductStore().UpdateAsync(product, default)).IsActive);
        Assert.Contains(await new ProductQueries(db).GetAllAsync(default), item => item.Id == product.Id && !item.IsActive);
    }

    [SqlServerFact]
    public async Task MissingResourcesAreRejectedByWriteStores()
    {
        var category = Category.Create("Missing", null);
        var product = Product.Create("Missing", null, "Missing", 1, database.CategoryId);
        Assert.Equal("CATEGORY_NOT_FOUND", await CaptureErrorAsync(() => CategoryStore.UpdateAsync(category, default)));
        Assert.Equal("CATEGORY_NOT_FOUND", await CaptureErrorAsync(() => CategoryStore.DeactivateAsync(category.Id, default)));
        Assert.Equal("PRODUCT_NOT_FOUND", await CaptureErrorAsync(() => database.CreateProductStore().UpdateAsync(product, default)));
        Assert.Equal("PRODUCT_NOT_FOUND", await CaptureErrorAsync(() => database.CreateProductStore().DeactivateAsync(product.Id, default)));
    }

    [SqlServerFact]
    public async Task CategoryDeletionAndProductCreationCannotViolateActiveCategoryRule()
    {
        var category = await CreateCategoryAsync();
        var product = Product.Create("Concurrent", null, Guid.NewGuid().ToString(), 1, category.Id);
        var outcomes = await RunCategoryRaceAsync(category.Id,
            () => CategoryStore.DeactivateAsync(category.Id, default),
            () => database.CreateProductStore().CreateAsync(product, default));
        await AssertCategoryAssignmentRaceAsync(category.Id, outcomes);
    }

    [SqlServerFact]
    public async Task CategoryDeletionAndProductReassignmentCannotViolateActiveCategoryRule()
    {
        var category = await CreateCategoryAsync();
        var product = await database.CreateProductAsync();
        product.Update(product.Name, null, product.Sku, product.Price, category.Id);
        var outcomes = await RunCategoryRaceAsync(category.Id,
            () => CategoryStore.DeactivateAsync(category.Id, default),
            () => database.CreateProductStore().UpdateAsync(product, default));
        await AssertCategoryAssignmentRaceAsync(category.Id, outcomes);
    }

    private async Task AssertCategoryAssignmentRaceAsync(Guid categoryId, string?[] outcomes)
    {
        await using var db = database.CreateReadContext();
        var active = (await new CategoryQueries(db).GetByIdAsync(categoryId, default))!.IsActive;
        if (active)
        {
            Assert.Equal(new string?[] { "CATEGORY_IN_USE", null }, outcomes);
            Assert.Equal(1, await db.Products.CountAsync(product => product.CategoryId == categoryId && product.IsActive));
        }
        else
        {
            Assert.Equal(new string?[] { null, "CATEGORY_INACTIVE" }, outcomes);
            Assert.False(await db.Products.AnyAsync(product => product.CategoryId == categoryId && product.IsActive));
        }
    }

    private async Task<string?[]> RunCategoryRaceAsync(Guid categoryId, Func<Task> delete, Func<Task> assign)
    {
        await using var blocker = database.CreateReadContext();
        await using var transaction = await blocker.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await blocker.Categories.FromSqlInterpolated($"SELECT * FROM Categories WITH (XLOCK, HOLDLOCK) WHERE Id = {categoryId}").AsNoTracking().SingleAsync();
        var first = CaptureErrorAsync(delete);
        var second = CaptureErrorAsync(assign);
        try
        {
            await using var observer = database.CreateReadContext();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (await observer.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM sys.dm_exec_requests WHERE database_id = DB_ID() AND wait_type LIKE 'LCK_M%'").SingleAsync(timeout.Token) < 2)
                await Task.Delay(25, timeout.Token);
        }
        finally
        {
            await transaction.RollbackAsync();
            await Task.WhenAll(first, second);
        }
        return [await first, await second];
    }

    private async Task<Category> CreateCategoryAsync()
    {
        var category = Category.Create(Guid.NewGuid().ToString(), null);
        await CategoryStore.CreateAsync(category, default);
        return category;
    }

    private async Task<Product> CreateProductAsync(Guid categoryId)
    {
        var product = Product.Create("Product", null, Guid.NewGuid().ToString(), 1, categoryId);
        await database.CreateProductStore().CreateAsync(product, default);
        return product;
    }

    private static async Task<string?> CaptureErrorAsync(Func<Task> action)
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
