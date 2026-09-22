using InventoryManagement.Domain.Common;

namespace InventoryManagement.Domain.Products;

public sealed class Product
{
    private Product() { }

    private Product(Guid id, string name, string? description, string sku, decimal price, Guid categoryId)
    {
        Id = id;
        SetDetails(name, description, sku, price, categoryId);
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public Guid CategoryId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Product Create(string name, string? description, string sku, decimal price, Guid categoryId) =>
        new(Guid.NewGuid(), name, description, sku, price, categoryId);

    public void Update(string name, string? description, string sku, decimal price, Guid categoryId)
    {
        SetDetails(name, description, sku, price, categoryId);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void EnsureActive()
    {
        if (!IsActive)
            throw new DomainException("PRODUCT_INACTIVE", "The product is inactive.");
    }

    private void SetDetails(string name, string? description, string sku, decimal price, Guid categoryId)
    {
        var validName = TextValidation.Required(name, 150, "INVALID_PRODUCT_NAME", "Product name");
        var validDescription = TextValidation.Optional(description, 1000, "INVALID_PRODUCT_DESCRIPTION", "Product description");
        var validSku = TextValidation.Required(sku, 80, "INVALID_PRODUCT_SKU", "Product SKU").ToUpperInvariant();
        if (price < 0 || price > 9999999999999999.99m || decimal.Round(price, 2) != price)
            throw new DomainException("INVALID_PRODUCT_PRICE", "Product price must be between 0 and 9999999999999999.99 with at most two decimal places.");
        if (categoryId == Guid.Empty)
            throw new DomainException("INVALID_CATEGORY_ID", "A category identifier is required.");
        Name = validName;
        Description = validDescription;
        Sku = validSku;
        Price = price;
        CategoryId = categoryId;
    }
}
