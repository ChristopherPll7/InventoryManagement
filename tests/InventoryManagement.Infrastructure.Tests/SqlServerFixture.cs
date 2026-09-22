using Dapper;
using InventoryManagement.Domain.Categories;
using InventoryManagement.Domain.Products;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Tests;

public sealed class SqlServerFixture : IAsyncLifetime
{
    private HashSet<Guid> originalProducts = [];
    private HashSet<Guid> originalCategories = [];
    private HashSet<Guid> originalMovements = [];
    private bool initialized;
    public string ConnectionString { get; private set; } = string.Empty;
    public Guid CategoryId { get; private set; }

    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable("INVENTORY_TEST_SQL_CONNECTION");
        if (string.IsNullOrWhiteSpace(configured))
            return;
        var builder = new SqlConnectionStringBuilder(configured);
        if (string.IsNullOrWhiteSpace(builder.InitialCatalog) ||
            new[] { "master", "model", "msdb", "tempdb" }.Contains(builder.InitialCatalog, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("SQL tests require an explicitly named existing application database.");
        ConnectionString = builder.ConnectionString;
        await new DatabaseInitializer(new SqlConnectionFactory(ConnectionString)).InitializeAsync(default);
        await using var db = CreateReadContext();
        originalProducts = (await db.Products.Select(product => product.Id).ToListAsync()).ToHashSet();
        originalCategories = (await db.Categories.Select(category => category.Id).ToListAsync()).ToHashSet();
        originalMovements = (await db.InventoryMovements.Select(movement => movement.Id).ToListAsync()).ToHashSet();
        initialized = true;
        var category = Category.Create($"SQL test category {Guid.NewGuid():N}", null);
        await new CategoryCommandStore(new SqlConnectionFactory(ConnectionString)).CreateAsync(category, default);
        CategoryId = category.Id;
    }

    public async Task DisposeAsync()
    {
        if (!initialized)
            return;
        await using var db = CreateReadContext();
        var movements = (await db.InventoryMovements.Select(item => item.Id).ToListAsync()).Where(id => !originalMovements.Contains(id)).ToArray();
        var products = (await db.Products.Select(item => item.Id).ToListAsync()).Where(id => !originalProducts.Contains(id)).ToArray();
        var categories = (await db.Categories.Select(item => item.Id).ToListAsync()).Where(id => !originalCategories.Contains(id)).ToArray();
        await DeleteRowsAsync("DELETE FROM dbo.InventoryMovements WHERE Id IN @Ids", movements);
        await DeleteRowsAsync("DELETE FROM dbo.Products WHERE Id IN @Ids", products);
        await DeleteRowsAsync("DELETE FROM dbo.Categories WHERE Id IN @Ids", categories);
        Assert.True(originalMovements.SetEquals(await db.InventoryMovements.Select(item => item.Id).ToListAsync()), "Movement cleanup must preserve the original rows.");
        Assert.True(originalProducts.SetEquals(await db.Products.Select(item => item.Id).ToListAsync()), "Product cleanup must preserve the original rows.");
        Assert.True(originalCategories.SetEquals(await db.Categories.Select(item => item.Id).ToListAsync()), "Category cleanup must preserve the original rows.");
        Console.WriteLine($"SQL cleanup: removed {movements.Length} movements, {products.Length} products and {categories.Length} categories; existing database preserved.");
    }

    private async Task DeleteRowsAsync(string sql, Guid[] ids)
    {
        foreach (var batch in ids.Chunk(500))
            await ExecuteAsync(sql, new { Ids = batch });
    }

    public InventoryReadDbContext CreateReadContext() =>
        new(new DbContextOptionsBuilder<InventoryReadDbContext>().UseSqlServer(ConnectionString).Options);

    public ProductCommandStore CreateProductStore() => new(new SqlConnectionFactory(ConnectionString));
    public InventoryCommandStore CreateInventoryStore() => new(new SqlConnectionFactory(ConnectionString));

    public async Task<Product> CreateProductAsync()
    {
        var product = Product.Create("Contract product", null, Guid.NewGuid().ToString("N"), 1, CategoryId);
        await CreateProductStore().CreateAsync(product, default);
        return product;
    }

    public async Task ExecuteAsync(string sql, object parameters)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.ExecuteAsync(sql, parameters);
    }
}
