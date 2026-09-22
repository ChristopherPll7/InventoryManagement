using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.Inventory;

namespace InventoryManagement.Api.Tests;

public sealed class InventoryQueriesStub : IInventoryQueries
{
    public ProductInventoryDto? Inventory { get; set; }
    public IReadOnlyCollection<InventoryMovementDto> Items { get; set; } = [];
    public InventoryMovementFilter? LastFilter { get; private set; }
    public Guid? LastProductId { get; private set; }

    public Task<ProductInventoryDto?> GetProductInventoryAsync(Guid productId, CancellationToken cancellationToken)
    {
        LastProductId = productId;
        return Task.FromResult(Inventory?.ProductId == productId ? Inventory : null);
    }

    public Task<PagedResult<InventoryMovementDto>?> GetMovementsAsync(Guid productId, InventoryMovementFilter filter, CancellationToken cancellationToken)
    {
        LastProductId = productId;
        LastFilter = filter;
        return Task.FromResult(Inventory?.ProductId == productId
            ? new PagedResult<InventoryMovementDto>(Items, filter.Page, filter.PageSize, Items.Count) : null);
    }
}
