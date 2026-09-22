using System.Data.SqlTypes;
using InventoryManagement.Application.Inventory;
using InventoryManagement.Domain.Inventory;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Tests;

[Collection("SqlServer")]
public sealed class InventoryReadPersistenceTests(SqlServerFixture database)
{
    [SqlServerFact]
    public async Task BalanceAndHistoryRemainAvailableAfterDeactivation()
    {
        var product = await database.CreateProductAsync();
        var movement = InventoryMovement.Create(product.Id, InventoryMovementType.Entry, 4, "Initial inventory");
        await database.CreateInventoryStore().RegisterAsync(movement, default);
        await database.CreateProductStore().DeactivateAsync(product.Id, default);
        await using var db = database.CreateReadContext();
        var queries = new InventoryQueries(db);
        var balance = await queries.GetProductInventoryAsync(product.Id, default);
        Assert.NotNull(balance);
        Assert.Equal(product.Id, balance.ProductId);
        Assert.Equal(product.Sku, balance.Sku);
        Assert.Equal(product.Name, balance.Name);
        Assert.Equal(4, balance.CurrentStock);
        var history = await queries.GetMovementsAsync(product.Id, new InventoryMovementFilter(), default);
        Assert.Equal(movement.Id, Assert.Single(history!.Items).Id);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [SqlServerFact]
    public async Task MissingProductDiffersFromExistingProductWithoutMovements()
    {
        var product = await database.CreateProductAsync();
        await using var db = database.CreateReadContext();
        var queries = new InventoryQueries(db);
        Assert.Null(await queries.GetProductInventoryAsync(Guid.NewGuid(), default));
        Assert.Null(await queries.GetMovementsAsync(Guid.NewGuid(), new InventoryMovementFilter(), default));
        Assert.Equal(0, (await queries.GetProductInventoryAsync(product.Id, default))!.CurrentStock);
        var empty = await queries.GetMovementsAsync(product.Id, new InventoryMovementFilter(), default);
        Assert.NotNull(empty);
        Assert.Empty(empty.Items);
        Assert.Equal(0, empty.TotalCount);
        Assert.Equal(0, empty.TotalPages);
    }

    [SqlServerFact]
    public async Task PagesUseNewestFirstOrderAndSqlGuidTieBreakerWithoutOverlap()
    {
        var product = await database.CreateProductAsync();
        var movements = await SeedHistoryAsync(product.Id);
        await using var db = database.CreateReadContext();
        var queries = new InventoryQueries(db);
        var first = (await queries.GetMovementsAsync(product.Id, new InventoryMovementFilter(pageSize: 2), default))!;
        var second = (await queries.GetMovementsAsync(product.Id, new InventoryMovementFilter(page: 2, pageSize: 2), default))!;
        var third = (await queries.GetMovementsAsync(product.Id, new InventoryMovementFilter(page: 3, pageSize: 2), default))!;
        var expected = movements.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => new SqlGuid(item.Id)).Select(item => item.Id);
        Assert.Equal(expected, first.Items.Concat(second.Items).Concat(third.Items).Select(item => item.Id));
        Assert.Equal(5, first.TotalCount);
        Assert.Equal(3, first.TotalPages);
        Assert.Equal(2, first.Items.Count);
        Assert.Single(third.Items);
        var repeated = (await queries.GetMovementsAsync(product.Id, new InventoryMovementFilter(pageSize: 2), default))!;
        Assert.Equal(first.Items, repeated.Items);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [SqlServerFact]
    public async Task TypeAndDateFiltersAreInclusiveAndIsolateTheProduct()
    {
        var product = await database.CreateProductAsync();
        var movements = await SeedHistoryAsync(product.Id);
        var other = await database.CreateProductAsync();
        await SeedHistoryAsync(other.Id);
        await using var db = database.CreateReadContext();
        var queries = new InventoryQueries(db);
        var instant = movements[1].CreatedAt;
        var range = (await queries.GetMovementsAsync(product.Id,
            new InventoryMovementFilter(startDate: instant.ToOffset(TimeSpan.FromHours(-6)), endDate: instant), default))!;
        Assert.Equal(2, range.TotalCount);
        Assert.All(range.Items, item => Assert.Equal(product.Id, item.ProductId));
        var filtered = (await queries.GetMovementsAsync(product.Id,
            new InventoryMovementFilter(InventoryMovementType.Exit, instant, instant), default))!;
        Assert.Equal(movements[2].Id, Assert.Single(filtered.Items).Id);
        Assert.Equal(1, filtered.TotalCount);
        var startOnly = (await queries.GetMovementsAsync(product.Id, new InventoryMovementFilter(startDate: instant), default))!;
        Assert.Equal(4, startOnly.TotalCount);
        var endOnly = (await queries.GetMovementsAsync(product.Id, new InventoryMovementFilter(endDate: instant), default))!;
        Assert.Equal(3, endOnly.TotalCount);
    }

    [SqlServerFact]
    public async Task OutOfRangePageIsEmptyButRetainsFilteredTotal()
    {
        var product = await database.CreateProductAsync();
        var movements = await SeedHistoryAsync(product.Id);
        await using var db = database.CreateReadContext();
        var queries = new InventoryQueries(db);
        var page = (await queries.GetMovementsAsync(product.Id,
            new InventoryMovementFilter(InventoryMovementType.Exit, page: 2, pageSize: 1), default))!;
        Assert.Empty(page.Items);
        Assert.Equal(1, page.TotalCount);
        Assert.Equal(1, page.TotalPages);
        Assert.Equal(2, page.Page);
        var noMatches = (await queries.GetMovementsAsync(product.Id,
            new InventoryMovementFilter(startDate: movements.Max(item => item.CreatedAt).AddDays(1)), default))!;
        Assert.Empty(noMatches.Items);
        Assert.Equal(0, noMatches.TotalCount);
    }

    private async Task<List<InventoryManagement.Application.Abstractions.InventoryMovementDto>> SeedHistoryAsync(Guid productId)
    {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var hourOffsets = new[] { 0, 1, 1, 2, 3 };
        var result = new List<InventoryManagement.Application.Abstractions.InventoryMovementDto>();
        for (var index = 0; index < hourOffsets.Length; index++)
        {
            var type = index == 2 ? InventoryMovementType.Exit : InventoryMovementType.Entry;
            var movement = InventoryMovement.Create(productId, type, 1, "History test");
            await database.CreateInventoryStore().RegisterAsync(movement, default);
            var createdAt = start.AddHours(hourOffsets[index]);
            await database.ExecuteAsync("UPDATE InventoryMovements SET CreatedAt = @CreatedAt WHERE Id = @Id", new { movement.Id, CreatedAt = createdAt });
            result.Add(new(movement.Id, productId, type, 1, movement.Reason, createdAt));
        }
        return result;
    }
}
