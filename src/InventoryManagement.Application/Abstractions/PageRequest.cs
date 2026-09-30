using InventoryManagement.Domain.Common;

namespace InventoryManagement.Application.Abstractions;

public sealed class PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 100;
    public const int MaximumPage = 10000;

    public PageRequest(int page = 1, int pageSize = DefaultPageSize)
    {
        if (page < 1 || page > MaximumPage || pageSize < 1 || pageSize > MaximumPageSize)
            throw new DomainException(ErrorCodes.InvalidPagination, $"Page must be between 1 and {MaximumPage}, and page size between 1 and {MaximumPageSize}.");
        Page = page;
        PageSize = pageSize;
    }

    public int Page { get; }
    public int PageSize { get; }
    public int Offset => (Page - 1) * PageSize;
}
