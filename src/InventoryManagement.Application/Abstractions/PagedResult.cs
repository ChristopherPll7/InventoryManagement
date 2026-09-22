namespace InventoryManagement.Application.Abstractions;

public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, long TotalCount)
{
    public long TotalPages => TotalCount / PageSize + (TotalCount % PageSize == 0 ? 0 : 1);
}
