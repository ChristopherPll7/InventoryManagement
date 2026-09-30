using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.Inventory;
using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Inventory;

namespace InventoryManagement.Application.Tests;

public sealed class InventoryQueryTests
{
    [Theory]
    [InlineData(0, 20)]
    [InlineData(-1, 20)]
    [InlineData(10001, 20)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    [InlineData(1, 101)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public void InvalidPaginationIsRejected(int page, int pageSize) =>
        Assert.Equal("INVALID_PAGINATION", Assert.Throws<DomainException>(() => new InventoryMovementFilter(page: page, pageSize: pageSize)).Code);

    [Fact]
    public void DefaultsAndUpperBoundsAreDefined()
    {
        var defaults = new InventoryMovementFilter();
        Assert.Equal(1, defaults.Page);
        Assert.Equal(20, defaults.PageSize);
        Assert.Equal(0, defaults.Offset);
        Assert.Equal(999900, new InventoryMovementFilter(page: 10000, pageSize: 100).Offset);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void UndefinedTypeIsRejected(int type) =>
        Assert.Equal("INVALID_INVENTORY_TYPE", Assert.Throws<DomainException>(() => new InventoryMovementFilter((InventoryMovementType)type)).Code);

    [Fact]
    public void ReversedDatesAreRejectedButEquivalentInstantsAreAllowed()
    {
        var start = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        Assert.Equal("INVALID_DATE_RANGE", Assert.Throws<DomainException>(() => new InventoryMovementFilter(startDate: start, endDate: start.AddTicks(-1))).Code);
        var filter = new InventoryMovementFilter(startDate: start, endDate: start.ToOffset(TimeSpan.FromHours(-6)));
        Assert.Equal(filter.StartDate, filter.EndDate);
    }

    [Fact]
    public async Task BalanceQueryReturnsProductIdentityAndZeroBalance()
    {
        var inventory = new ProductInventoryDto(Guid.NewGuid(), "SKU", "Product", 0);
        var queries = new InventoryQueriesStub { Inventory = inventory };
        var result = await new GetProductInventoryQueryHandler(queries).HandleAsync(new GetProductInventoryQuery(inventory.ProductId), default);
        Assert.True(result.IsSuccess);
        Assert.Equal(inventory, result.Value);
        Assert.Equal(inventory.ProductId, queries.LastProductId);
    }

    [Fact]
    public async Task MissingProductIsNotFoundForBothQueries()
    {
        var queries = new InventoryQueriesStub();
        var id = Guid.NewGuid();
        var balanceResult = await new GetProductInventoryQueryHandler(queries).HandleAsync(new GetProductInventoryQuery(id), default);
        var historyError = await new GetProductInventoryMovementsQueryHandler(queries).HandleAsync(new GetProductInventoryMovementsQuery(id), default);
        Assert.True(balanceResult.IsFailure);
        Assert.Equal("PRODUCT_NOT_FOUND", balanceResult.Error.Code);
        Assert.Equal("PRODUCT_NOT_FOUND", historyError.Error.Code);
    }

    [Fact]
    public async Task EmptyHistoryIsSuccessfulAndFiltersReachPersistence()
    {
        var inventory = new ProductInventoryDto(Guid.NewGuid(), "SKU", "Product", 0);
        var queries = new InventoryQueriesStub { Inventory = inventory };
        var filter = new InventoryMovementFilter(InventoryMovementType.Exit, page: 2, pageSize: 5);
        var result = await new GetProductInventoryMovementsQueryHandler(queries).HandleAsync(new GetProductInventoryMovementsQuery(inventory.ProductId, filter.Type, filter.StartDate, filter.EndDate, filter.Page, filter.PageSize), default);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
        Assert.Equal(0, result.Value.TotalPages);
        Assert.Equal(2, result.Value.Page);
        Assert.Equal(filter.Type, queries.LastFilter!.Type);
        Assert.Equal(filter.PageSize, queries.LastFilter.PageSize);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(20, 1)]
    [InlineData(21, 2)]
    public void TotalPagesRoundsUp(long count, long expected) =>
        Assert.Equal(expected, new PagedResult<InventoryMovementDto>([], 1, 20, count).TotalPages);
}
