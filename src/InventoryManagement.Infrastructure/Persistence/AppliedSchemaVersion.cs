namespace InventoryManagement.Infrastructure.Persistence;

public sealed record AppliedSchemaVersion(int Version, string Name, string Checksum);
