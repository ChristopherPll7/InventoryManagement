using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Inventory;

namespace InventoryManagement.Domain.Tests;

public sealed class InventoryMovementTests
{
    [Fact]
    public void EntryIncreasesStock()
    {
        var movement = InventoryMovement.Create(Guid.NewGuid(), InventoryMovementType.Entry, 5, null);
        Assert.Equal(15, movement.CalculateResultingStock(10));
    }

    [Fact]
    public void ExitDecreasesStock()
    {
        var movement = InventoryMovement.Create(Guid.NewGuid(), InventoryMovementType.Exit, 4, null);
        Assert.Equal(6, movement.CalculateResultingStock(10));
    }

    [Fact]
    public void ExitWithInsufficientStockFails()
    {
        var movement = InventoryMovement.Create(Guid.NewGuid(), InventoryMovementType.Exit, 5, null);
        var exception = Assert.Throws<DomainException>(() => movement.CalculateResultingStock(3));
        Assert.Equal("INSUFFICIENT_STOCK", exception.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveQuantityFails(int quantity)
    {
        var exception = Assert.Throws<DomainException>(() => InventoryMovement.Create(Guid.NewGuid(), InventoryMovementType.Entry, quantity, null));
        Assert.Equal("INVALID_INVENTORY_QUANTITY", exception.Code);
    }
}
