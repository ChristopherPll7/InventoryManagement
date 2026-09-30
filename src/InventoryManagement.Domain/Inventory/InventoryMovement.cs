using InventoryManagement.Domain.Common;

namespace InventoryManagement.Domain.Inventory;

public sealed class InventoryMovement
{
    private InventoryMovement() { }

    private InventoryMovement(Guid productId, InventoryMovementType type, int quantity, string? reason)
    {
        if (productId == Guid.Empty) throw new DomainException(ErrorCodes.InvalidProductId, "A product identifier is required.");
        if (!Enum.IsDefined(type)) throw new DomainException(ErrorCodes.InvalidInventoryType, "Movement type must be Entry or Exit.");
        if (quantity <= 0) throw new DomainException(ErrorCodes.InvalidInventoryQuantity, "Quantity must be greater than zero.");
        Id = Guid.NewGuid();
        ProductId = productId;
        Type = type;
        Quantity = quantity;
        Reason = TextValidation.Optional(reason, 500, ErrorCodes.InvalidInventoryReason, "Movement reason");
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public InventoryMovementType Type { get; private set; }
    public int Quantity { get; private set; }
    public string? Reason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static InventoryMovement Create(Guid productId, InventoryMovementType type, int quantity, string? reason) =>
        new(productId, type, quantity, reason);

    public int CalculateResultingStock(int currentStock)
    {
        if (currentStock < 0)
            throw new DomainException(ErrorCodes.InvalidInventoryStock, "Current stock cannot be negative.");
        var resultingStock = Type == InventoryMovementType.Entry ? (long)currentStock + Quantity : (long)currentStock - Quantity;
        if (resultingStock < 0)
            throw new DomainException(ErrorCodes.InsufficientStock, "The requested quantity exceeds the available stock.");
        if (resultingStock > int.MaxValue)
            throw new DomainException(ErrorCodes.InventoryStockOverflow, "The movement would exceed the maximum supported stock.");
        return (int)resultingStock;
    }
}
