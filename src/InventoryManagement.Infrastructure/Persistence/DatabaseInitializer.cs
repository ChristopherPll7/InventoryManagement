namespace InventoryManagement.Infrastructure.Persistence;

public sealed class DatabaseInitializer(SqlConnectionFactory connectionFactory)
{
    public Task InitializeAsync(CancellationToken cancellationToken) =>
        new SchemaMigrator(connectionFactory).ApplyAsync(MigrationCatalog.Load(), cancellationToken);
}
