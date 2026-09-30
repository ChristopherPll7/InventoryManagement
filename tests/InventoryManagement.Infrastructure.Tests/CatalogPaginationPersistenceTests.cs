using InventoryManagement.Application.Abstractions;
using InventoryManagement.Domain.Categories;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Tests;

[Collection("SqlServer")]
public sealed class CatalogPaginationPersistenceTests(SqlServerFixture database)
{
    [SqlServerFact]
    public async Task ProductPagesCoverOrderedRowsWithoutDuplicatesAndKeepInactiveProducts()
    {
        var first = await database.CreateProductAsync();
        await database.CreateProductAsync();
        await database.CreateProductStore().DeactivateAsync(first.Id, default);
        await using var db = database.CreateReadContext();
        var expected = await db.Products.AsNoTracking().OrderBy(x => x.Name).ThenBy(x => x.Id).Select(x => x.Id).ToArrayAsync();
        var queries = new ProductQueries(db);
        var ids = new List<Guid>();
        var firstPage = await queries.GetAllAsync(new PageRequest(1, 2), default);
        for (var page = 1; page <= firstPage.TotalPages; page++)
        {
            var result = await queries.GetAllAsync(new PageRequest(page, 2), default);
            Assert.Equal(expected.Length, result.TotalCount);
            Assert.Equal(page, result.Page);
            Assert.Equal(2, result.PageSize);
            Assert.InRange(result.Items.Count, 1, 2);
            ids.AddRange(result.Items.Select(x => x.Id));
        }
        Assert.Equal(expected, ids);
        Assert.Contains(first.Id, ids);
        var beyond = await queries.GetAllAsync(new PageRequest((int)firstPage.TotalPages + 1, 2), default);
        Assert.Empty(beyond.Items);
        Assert.Equal(expected.Length, beyond.TotalCount);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [SqlServerFact]
    public async Task CategoryPagesCoverOrderedRowsAndKeepInactiveCategories()
    {
        var category = Category.Create($"Pagination {Guid.NewGuid():N}", null);
        var store = new CategoryCommandStore(new SqlConnectionFactory(database.ConnectionString));
        await store.CreateAsync(category, default);
        await store.DeactivateAsync(category.Id, default);
        await using var db = database.CreateReadContext();
        var expected = await db.Categories.AsNoTracking().OrderBy(x => x.Name).ThenBy(x => x.Id).Select(x => x.Id).ToArrayAsync();
        var queries = new CategoryQueries(db);
        var ids = new List<Guid>();
        for (var page = 1; page <= expected.Length; page++)
        {
            var result = await queries.GetAllAsync(new PageRequest(page, 1), default);
            Assert.Equal(expected.Length, result.TotalCount);
            Assert.Equal(expected.Length, result.TotalPages);
            ids.Add(Assert.Single(result.Items).Id);
        }
        Assert.Equal(expected, ids);
        Assert.Contains(category.Id, ids);
        var beyond = await queries.GetAllAsync(new PageRequest(expected.Length + 1, 1), default);
        Assert.Empty(beyond.Items);
        Assert.Equal(expected.Length, beyond.TotalCount);
        Assert.Empty(db.ChangeTracker.Entries());
    }
}
