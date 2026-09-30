using InventoryManagement.Domain.Common;

namespace InventoryManagement.Domain.Products;

public sealed record ProductDetails
{
    public ProductDetails(string name, string? description, string sku, decimal price, Guid categoryId)
    {
        Name = TextValidation.Required(name, 150, ErrorCodes.InvalidProductName, "Product name");
        Description = TextValidation.Optional(description, 1000, ErrorCodes.InvalidProductDescription, "Product description");
        Sku = TextValidation.Required(sku, 80, ErrorCodes.InvalidProductSku, "Product SKU").ToUpperInvariant();
        if (price < 0 || price > 9999999999999999.99m || decimal.Round(price, 2) != price)
            throw new DomainException(ErrorCodes.InvalidProductPrice, "Product price must be between 0 and 9999999999999999.99 with at most two decimal places.");
        if (categoryId == Guid.Empty)
            throw new DomainException(ErrorCodes.InvalidCategoryId, "A category identifier is required.");
        Price = price;
        CategoryId = categoryId;
    }

    public string Name { get; }
    public string? Description { get; }
    public string Sku { get; }
    public decimal Price { get; }
    public Guid CategoryId { get; }
}
