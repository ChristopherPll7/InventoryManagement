using System.Security.Cryptography;
using System.Text;

namespace InventoryManagement.Infrastructure.Persistence;

public sealed record SchemaMigration(int Version, string Name, string Sql)
{
    public string Checksum => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Sql.Replace("\r\n", "\n"))));
}
