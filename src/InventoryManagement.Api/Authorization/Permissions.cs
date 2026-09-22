namespace InventoryManagement.Api.Authorization;

public static class Permissions
{
    public const string ClaimType = "permissions";
    public const string ProductsRead = "products.read";
    public const string ProductsWrite = "products.write";
    public const string CategoriesRead = "categories.read";
    public const string CategoriesWrite = "categories.write";
    public const string InventoryRead = "inventory.read";
    public const string InventoryWrite = "inventory.write";

    public static IReadOnlyList<string> All { get; } =
        [ProductsRead, ProductsWrite, CategoriesRead, CategoriesWrite, InventoryRead, InventoryWrite];
}
