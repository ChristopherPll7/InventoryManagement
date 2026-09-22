using System.ComponentModel.DataAnnotations;
using InventoryManagement.Application.Inventory;
using InventoryManagement.Domain.Inventory;

namespace InventoryManagement.Api.Contracts;

public sealed class InventoryMovementHistoryRequest
{
    [RegularExpression("^(?i:Entry|Exit)$")]
    public string? Type { get; init; }
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = InventoryMovementFilter.DefaultPageSize;

    public InventoryMovementFilter ToFilter() =>
        new(Type is null ? null : Enum.Parse<InventoryMovementType>(Type, true), StartDate, EndDate, Page, PageSize);
}
