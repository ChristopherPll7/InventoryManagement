namespace InventoryManagement.Infrastructure.Persistence;

public static class MigrationCatalog
{
    public static IReadOnlyList<SchemaMigration> Load()
    {
        var assembly = typeof(MigrationCatalog).Assembly;
        return assembly.GetManifestResourceNames()
            .Where(name => name.Contains(".Migrations.V", StringComparison.Ordinal) && name.EndsWith(".sql", StringComparison.Ordinal))
            .Select(resource =>
            {
                var name = resource[(resource.IndexOf(".Migrations.", StringComparison.Ordinal) + ".Migrations.".Length)..];
                using var reader = new StreamReader(assembly.GetManifestResourceStream(resource)!);
                return new SchemaMigration(int.Parse(name.AsSpan(1, 3), System.Globalization.CultureInfo.InvariantCulture), name, reader.ReadToEnd());
            })
            .OrderBy(migration => migration.Version).ToArray();
    }
}
