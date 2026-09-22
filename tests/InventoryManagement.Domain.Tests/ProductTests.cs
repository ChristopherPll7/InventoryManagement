using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Products;

namespace InventoryManagement.Domain.Tests;

public sealed class ProductTests
{
    [Fact]
    public void ValidProductIsActiveByDefault()
    {
        var product = Product.Create("Keyboard", null, "KEY-001", 999.99m, Guid.NewGuid());
        Assert.True(product.IsActive);
    }

    [Fact]
    public void NegativePriceFails()
    {
        var exception = Assert.Throws<DomainException>(() => Product.Create("Keyboard", null, "KEY-001", -1, Guid.NewGuid()));
        Assert.Equal("INVALID_PRODUCT_PRICE", exception.Code);
    }
}
