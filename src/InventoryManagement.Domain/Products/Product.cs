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
            throw new DomainException(ErrorCodes.ProductInactive, "The product is inactive.");
    }

    private void SetDetails(string name, string? description, string sku, decimal price, Guid categoryId)
    {
        ApplyDetails(new ProductDetails(name, description, sku, price, categoryId));
    }

    public void Update(ProductDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);
        ApplyDetails(details);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void ApplyDetails(ProductDetails details)
    {
        Name = details.Name;
        Description = details.Description;
        Sku = details.Sku;
        Price = details.Price;
        CategoryId = details.CategoryId;
    }
}
