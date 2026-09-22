using InventoryManagement.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Tests;

[Collection("SqlServer")]
public sealed class SchemaMigrationTests(SqlServerFixture fixture)
{
    [SqlServerFact]
    public async Task Repeated_and_concurrent_deployment_preserves_the_journal()
    {
        var before = await ReadVersionsAsync();
        await Task.WhenAll(ApplyAsync(MigrationCatalog.Load()), ApplyAsync(MigrationCatalog.Load()));
        Assert.Equal(before, await ReadVersionsAsync());
        Assert.Equal(MigrationCatalog.Load().Count, before.Length);
    }

    [SqlServerFact]
    public async Task Edited_migration_is_rejected_without_changing_history()
    {
        var catalog = MigrationCatalog.Load().ToArray();
        catalog[^1] = catalog[^1] with { Sql = catalog[^1].Sql + "\n-- changed" };
        var before = await ReadVersionsAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => ApplyAsync(catalog));
        Assert.Equal(before, await ReadVersionsAsync());
    }

    [SqlServerFact]
    public async Task Older_release_cannot_apply_over_a_newer_database()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => ApplyAsync(MigrationCatalog.Load().Take(1).ToArray()));
    }

    [SqlServerFact]
    public async Task Failed_migration_rolls_back_ddl_and_journal()
    {
        var catalog = MigrationCatalog.Load();
        var table = $"SchemaFailureProbe_{Guid.NewGuid():N}";
        var failing = new SchemaMigration(catalog.Count + 1, "FailureProbe", $"CREATE TABLE dbo.{table} (Id int NOT NULL); THROW 51011, 'Injected migration failure', 1;");
        var before = await ReadVersionsAsync();
        var error = await Assert.ThrowsAsync<SqlException>(() => ApplyAsync([.. catalog, failing]));
        Assert.Equal(51011, error.Number);
        await using var db = fixture.CreateReadContext();
        Assert.Null(await db.Database.SqlQuery<int?>($"SELECT OBJECT_ID({"dbo." + table}) AS Value").SingleAsync());
        Assert.Equal(before, await ReadVersionsAsync());
    }

    private Task ApplyAsync(IReadOnlyList<SchemaMigration> migrations) =>
        new SchemaMigrator(new SqlConnectionFactory(fixture.ConnectionString)).ApplyAsync(migrations, default);

    private async Task<string[]> ReadVersionsAsync()
    {
        await using var db = fixture.CreateReadContext();
        return await db.Database.SqlQueryRaw<string>("SELECT CONCAT(Version, ':', Name, ':', Checksum, ':', CONVERT(nvarchar(50), AppliedAt, 127)) AS Value FROM dbo.SchemaVersions").OrderBy(value => value).ToArrayAsync();
    }
}
