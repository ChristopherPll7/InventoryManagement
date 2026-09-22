using Microsoft.Data.SqlClient;

namespace InventoryManagement.Infrastructure.Persistence;

public sealed class SqlConnectionFactory(string connectionString)
{
    public SqlConnection Create() => new(connectionString);
}
