using InventoryManagement.Domain.Common;
using InventoryManagement.Application.Abstractions;
using InventoryManagement.Domain.Inventory;

namespace InventoryManagement.Application.Inventory;

public sealed class InventoryMovementFilter
{
    public const int DefaultPageSize = PageRequest.DefaultPageSize;
    public const int MaximumPageSize = PageRequest.MaximumPageSize;
    public const int MaximumPage = PageRequest.MaximumPage;

    public InventoryMovementFilter(InventoryMovementType? type = null, DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null, int page = 1, int pageSize = DefaultPageSize)
    {
        if (type.HasValue && !Enum.IsDefined(type.Value))
            throw new DomainException(ErrorCodes.InvalidInventoryType, "Movement type must be Entry or Exit.");
        if (startDate.HasValue && endDate.HasValue && startDate > endDate)
            throw new DomainException(ErrorCodes.InvalidDateRange, "Start date cannot be later than end date.");
        var pagination = new PageRequest(page, pageSize);
        Type = type;
        StartDate = startDate;
        EndDate = endDate;
        Page = pagination.Page;
        PageSize = pagination.PageSize;
    }

    public InventoryMovementType? Type { get; }
    public DateTimeOffset? StartDate { get; }
    public DateTimeOffset? EndDate { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int Offset => (Page - 1) * PageSize;
}
