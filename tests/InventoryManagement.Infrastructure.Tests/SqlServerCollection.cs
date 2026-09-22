namespace InventoryManagement.Infrastructure.Tests;

[CollectionDefinition("SqlServer", DisableParallelization = true)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>;
