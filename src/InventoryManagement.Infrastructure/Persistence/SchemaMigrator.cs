using System.Data;
using System.Data.Common;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Persistence;

public sealed class SchemaMigrator(SqlConnectionFactory connectionFactory)
{
    public async Task ApplyAsync(IReadOnlyList<SchemaMigration> migrations, CancellationToken cancellationToken)
    {
        ValidateCatalog(migrations);
        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await using var db = new InventoryReadDbContext(new DbContextOptionsBuilder<InventoryReadDbContext>().UseSqlServer(connection).Options);
        await db.Database.UseTransactionAsync(transaction, cancellationToken);
        try
        {
            await PrepareJournalAsync(connection, transaction, cancellationToken);
            var applied = await db.Database.SqlQueryRaw<AppliedSchemaVersion>("SELECT Version, Name, Checksum FROM dbo.SchemaVersions").ToListAsync(cancellationToken);
            ValidateHistory(migrations, applied);
            foreach (var migration in migrations.Where(candidate => applied.All(version => version.Version != candidate.Version)))
                await ApplyMigrationAsync(connection, transaction, migration, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static Task PrepareJournalAsync(DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            SET XACT_ABORT ON;
            DECLARE @lockResult int;
            EXEC @lockResult = sys.sp_getapplock @Resource = 'InventoryManagement.SchemaMigration',
                @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 30000;
            IF @lockResult < 0 THROW 51010, 'Could not acquire the schema migration lock.', 1;
            IF OBJECT_ID('dbo.SchemaVersions') IS NULL
                CREATE TABLE dbo.SchemaVersions (
                    Version int NOT NULL PRIMARY KEY,
                    Name nvarchar(200) NOT NULL,
                    Checksum char(64) NOT NULL,
                    AppliedAt datetimeoffset NOT NULL);
            """;
        return connection.ExecuteAsync(new CommandDefinition(sql, transaction: transaction, cancellationToken: cancellationToken));
    }

    private static async Task ApplyMigrationAsync(DbConnection connection, DbTransaction transaction, SchemaMigration migration, CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition(migration.Sql, transaction: transaction, cancellationToken: cancellationToken));
        const string sql = "INSERT INTO dbo.SchemaVersions (Version, Name, Checksum, AppliedAt) VALUES (@Version, @Name, @Checksum, SYSUTCDATETIME())";
        await connection.ExecuteAsync(new CommandDefinition(sql, migration, transaction, cancellationToken: cancellationToken));
    }

    private static void ValidateCatalog(IReadOnlyList<SchemaMigration> migrations)
    {
        if (migrations.Count == 0 || !migrations.Select(migration => migration.Version).SequenceEqual(Enumerable.Range(1, migrations.Count)))
            throw new InvalidOperationException("Schema migrations must be ordered, unique and consecutive starting at version 1.");
    }

    private static void ValidateHistory(IReadOnlyList<SchemaMigration> migrations, IReadOnlyCollection<AppliedSchemaVersion> applied)
    {
        foreach (var version in applied)
        {
            var migration = migrations.SingleOrDefault(candidate => candidate.Version == version.Version)
                ?? throw new InvalidOperationException($"Database migration {version.Version} is newer than or absent from this release.");
            if (migration.Name != version.Name || migration.Checksum != version.Checksum)
                throw new InvalidOperationException($"Database migration {version.Version} does not match this release. Applied migrations must not be edited.");
        }
        if (!applied.Select(version => version.Version).Order().SequenceEqual(Enumerable.Range(1, applied.Count)))
            throw new InvalidOperationException("The schema migration history contains gaps.");
    }
}
