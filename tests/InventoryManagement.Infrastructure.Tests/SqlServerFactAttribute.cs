namespace InventoryManagement.Infrastructure.Tests;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("INVENTORY_TEST_SQL_CONNECTION")))
            Skip = "Set INVENTORY_TEST_SQL_CONNECTION to the existing application database; tests modify data and remove newly created rows afterward.";
    }
}
