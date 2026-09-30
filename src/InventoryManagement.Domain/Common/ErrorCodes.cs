namespace InventoryManagement.Domain.Common;

public static class ErrorCodes
{
    public const string CategoryInactive = "CATEGORY_INACTIVE";
    public const string CategoryInUse = "CATEGORY_IN_USE";
    public const string CategoryNotFound = "CATEGORY_NOT_FOUND";
    public const string DuplicateCategoryName = "DUPLICATE_CATEGORY_NAME";
    public const string DuplicateProductSku = "DUPLICATE_PRODUCT_SKU";
    public const string InsufficientStock = "INSUFFICIENT_STOCK";
    public const string InvalidCategoryDescription = "INVALID_CATEGORY_DESCRIPTION";
    public const string InvalidCategoryId = "INVALID_CATEGORY_ID";
    public const string InvalidCategoryName = "INVALID_CATEGORY_NAME";
    public const string InvalidDateRange = "INVALID_DATE_RANGE";
    public const string InvalidInventoryQuantity = "INVALID_INVENTORY_QUANTITY";
    public const string InvalidInventoryReason = "INVALID_INVENTORY_REASON";
    public const string InvalidInventoryStock = "INVALID_INVENTORY_STOCK";
    public const string InvalidInventoryType = "INVALID_INVENTORY_TYPE";
    public const string InvalidPagination = "INVALID_PAGINATION";
    public const string InvalidProductDescription = "INVALID_PRODUCT_DESCRIPTION";
    public const string InvalidProductId = "INVALID_PRODUCT_ID";
    public const string InvalidProductName = "INVALID_PRODUCT_NAME";
    public const string InvalidProductPrice = "INVALID_PRODUCT_PRICE";
    public const string InvalidProductSku = "INVALID_PRODUCT_SKU";
    public const string InventoryStockOverflow = "INVENTORY_STOCK_OVERFLOW";
    public const string ProductInactive = "PRODUCT_INACTIVE";
    public const string ProductNotFound = "PRODUCT_NOT_FOUND";
}
