using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Inventory;

namespace InventoryManagement.Application.Inventory;

public sealed class InventoryMovementFilter
{
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 100;
    public const int MaximumPage = 10000;

    public InventoryMovementFilter(InventoryMovementType? type = null, DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null, int page = 1, int pageSize = DefaultPageSize)
    {
        if (type.HasValue && !Enum.IsDefined(type.Value))
            throw new DomainException("INVALID_INVENTORY_TYPE", "Movement type must be Entry or Exit.");
        if (startDate.HasValue && endDate.HasValue && startDate > endDate)
            throw new DomainException("INVALID_DATE_RANGE", "Start date cannot be later than end date.");
        if (page < 1 || page > MaximumPage || pageSize < 1 || pageSize > MaximumPageSize)
            throw new DomainException("INVALID_PAGINATION", $"Page must be between 1 and {MaximumPage}, and page size between 1 and {MaximumPageSize}.");
        Type = type;
        StartDate = startDate;
        EndDate = endDate;
        Page = page;
        PageSize = pageSize;
    }

    public InventoryMovementType? Type { get; }
    public DateTimeOffset? StartDate { get; }
    public DateTimeOffset? EndDate { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int Offset => (Page - 1) * PageSize;
}
