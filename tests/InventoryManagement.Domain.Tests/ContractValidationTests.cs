using InventoryManagement.Domain.Categories;
using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Inventory;
using InventoryManagement.Domain.Products;

namespace InventoryManagement.Domain.Tests;

public sealed class ContractValidationTests
{
    [Fact]
    public void DeactivatedProductRejectsNewMovements()
    {
        var product = Product.Create("Product", null, "SKU", 1, Guid.NewGuid());
        product.EnsureActive();
        product.Deactivate();
        var error = Assert.Throws<DomainException>(() => product.EnsureActive());
        Assert.Equal("PRODUCT_INACTIVE", error.Code);
    }

    [Fact]
    public void EmptyIdentifiersAreInvalidInput()
    {
        var productError = Assert.Throws<DomainException>(() => Product.Create("Product", null, "SKU", 1, Guid.Empty));
        var movementError = Assert.Throws<DomainException>(() => InventoryMovement.Create(Guid.Empty, InventoryMovementType.Entry, 1, null));
        Assert.Equal("INVALID_CATEGORY_ID", productError.Code);
        Assert.Equal("INVALID_PRODUCT_ID", movementError.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(-1)]
    public void UndefinedMovementTypeIsRejected(int type)
    {
        var error = Assert.Throws<DomainException>(() => InventoryMovement.Create(Guid.NewGuid(), (InventoryMovementType)type, 1, null));
        Assert.Equal("INVALID_INVENTORY_TYPE", error.Code);
    }

    [Fact]
    public void EntryCannotOverflowStock()
    {
        var movement = InventoryMovement.Create(Guid.NewGuid(), InventoryMovementType.Entry, 1, null);
        var error = Assert.Throws<DomainException>(() => movement.CalculateResultingStock(int.MaxValue));
        Assert.Equal("INVENTORY_STOCK_OVERFLOW", error.Code);
    }

    [Fact]
    public void MaximumStockAndExactExitAreSupported()
    {
        var entry = InventoryMovement.Create(Guid.NewGuid(), InventoryMovementType.Entry, int.MaxValue, null);
        var exit = InventoryMovement.Create(Guid.NewGuid(), InventoryMovementType.Exit, int.MaxValue, null);
        Assert.Equal(int.MaxValue, entry.CalculateResultingStock(0));
        Assert.Equal(0, exit.CalculateResultingStock(int.MaxValue));
    }

    [Fact]
    public void NegativeStartingStockIsRejected()
    {
        var movement = InventoryMovement.Create(Guid.NewGuid(), InventoryMovementType.Entry, 5, null);
        Assert.Throws<DomainException>(() => movement.CalculateResultingStock(-1));
    }

    [Fact]
    public void OversizedMovementReasonIsRejected()
    {
        var error = Assert.Throws<DomainException>(() => InventoryMovement.Create(Guid.NewGuid(), InventoryMovementType.Entry, 1, new string('x', 501)));
        Assert.Equal("INVALID_INVENTORY_REASON", error.Code);
    }

    [Theory]
    [InlineData("10000000000000000")]
    [InlineData("1.001")]
    public void UnrepresentablePriceIsRejected(string value)
    {
        var price = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        var error = Assert.Throws<DomainException>(() => Product.Create("Product", null, "SKU", price, Guid.NewGuid()));
        Assert.Equal("INVALID_PRODUCT_PRICE", error.Code);
    }

    [Fact]
    public void ProductAcceptsStorageBoundaries()
    {
        var product = Product.Create(new string('n', 150), new string('d', 1000), new string('s', 80), 9999999999999999.99m, Guid.NewGuid());
        Assert.Equal(150, product.Name.Length);
        Assert.Equal(new string('S', 80), product.Sku);
    }

    [Theory]
    [InlineData(151, 10, 10, "INVALID_PRODUCT_NAME")]
    [InlineData(10, 1001, 10, "INVALID_PRODUCT_DESCRIPTION")]
    [InlineData(10, 10, 81, "INVALID_PRODUCT_SKU")]
    public void ProductRejectsOversizedText(int nameLength, int descriptionLength, int skuLength, string code)
    {
        var error = Assert.Throws<DomainException>(() => Product.Create(new string('n', nameLength), new string('d', descriptionLength), new string('s', skuLength), 1, Guid.NewGuid()));
        Assert.Equal(code, error.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void MissingSkuIsRejected(string? sku)
    {
        var error = Assert.Throws<DomainException>(() => Product.Create("Product", null, sku!, 1, Guid.NewGuid()));
        Assert.Equal("INVALID_PRODUCT_SKU", error.Code);
    }

    [Fact]
    public void FailedUpdateDoesNotPartiallyChangeProduct()
    {
        var product = Product.Create("Original", null, "SKU", 10, Guid.NewGuid());
        Assert.Throws<DomainException>(() => product.Update("Changed", "Changed", "NEW", 1.001m, Guid.NewGuid()));
        Assert.Equal("Original", product.Name);
        Assert.Null(product.Description);
        Assert.Equal("SKU", product.Sku);
        Assert.Equal(10, product.Price);
        Assert.Null(product.UpdatedAt);
    }

    [Theory]
    [InlineData(151, 10, "INVALID_CATEGORY_NAME")]
    [InlineData(10, 501, "INVALID_CATEGORY_DESCRIPTION")]
    public void CategoryRejectsOversizedText(int nameLength, int descriptionLength, string code)
    {
        var error = Assert.Throws<DomainException>(() => Category.Create(new string('n', nameLength), new string('d', descriptionLength)));
        Assert.Equal(code, error.Code);
    }
}
