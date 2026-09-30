using InventoryManagement.Application.Categories;
using InventoryManagement.Application.Inventory;
using InventoryManagement.Application.Products;
using InventoryManagement.Domain.Categories;
using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Inventory;

namespace InventoryManagement.Application.Tests;

public sealed class HandlerResultTests
{
    [Theory]
    [InlineData(ErrorCodes.InsufficientStock)]
    [InlineData(ErrorCodes.ProductInactive)]
    [InlineData(ErrorCodes.ProductNotFound)]
    public async Task InventoryStoreConflictsBecomeFailures(string code)
    {
        var store = new InventoryCommandStoreStub { Error = new DomainException(code, "Movement rejected.") };
        var result = await new RegisterInventoryMovementCommandHandler(store).HandleAsync(
            new(Guid.NewGuid(), InventoryMovementType.Exit, 1, null), default);
        Assert.True(result.IsFailure);
        Assert.Equal(code, result.Error.Code);
        Assert.Equal("Movement rejected.", result.Error.Message);
    }

    [Fact]
    public async Task InvalidMovementDoesNotReachStore()
    {
        var store = new InventoryCommandStoreStub();
        var result = await new RegisterInventoryMovementCommandHandler(store).HandleAsync(
            new(Guid.NewGuid(), InventoryMovementType.Entry, 0, null), default);
        Assert.Equal(ErrorCodes.InvalidInventoryQuantity, result.Error.Code);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task ValidMovementReturnsStoreValue()
    {
        var store = new InventoryCommandStoreStub();
        var result = await new RegisterInventoryMovementCommandHandler(store).HandleAsync(
            new(Guid.NewGuid(), InventoryMovementType.Entry, 5, null), default);
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.CurrentStock);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TechnicalFailuresAndCancellationAreNotBusinessResults(bool cancelled)
    {
        Exception expected = cancelled ? new OperationCanceledException() : new InvalidOperationException("Technical failure.");
        var store = new InventoryCommandStoreStub { Error = expected };
        var actual = await Record.ExceptionAsync(() => new RegisterInventoryMovementCommandHandler(store).HandleAsync(
            new(Guid.NewGuid(), InventoryMovementType.Entry, 1, null), default));
        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task MissingDeactivationsReturnFailures()
    {
        var product = await new DeleteProductCommandHandler(new ProductPersistenceStub()).HandleAsync(new(Guid.NewGuid()), default);
        var category = await new DeleteCategoryCommandHandler(new CategoryPersistenceStub()).HandleAsync(new(Guid.NewGuid()), default);
        Assert.Equal(ErrorCodes.ProductNotFound, product.Error.Code);
        Assert.Equal(ErrorCodes.CategoryNotFound, category.Error.Code);
    }

    [Fact]
    public async Task CategoryReadAndDeactivationReturnSuccess()
    {
        var store = new CategoryPersistenceStub();
        var category = Category.Create("Tools", null);
        await store.CreateAsync(category, default);
        var read = await new GetCategoryByIdQueryHandler(store).HandleAsync(new(category.Id), default);
        Assert.Equal(category.Id, read.Value.Id);
        var deleted = await new DeleteCategoryCommandHandler(store).HandleAsync(new(category.Id), default);
        Assert.True(deleted.IsSuccess);
        Assert.False(category.IsActive);
    }

    [Fact]
    public async Task MissingCategoryReadReturnsFailure()
    {
        var result = await new GetCategoryByIdQueryHandler(new CategoryPersistenceStub()).HandleAsync(new(Guid.NewGuid()), default);
        Assert.Equal(ErrorCodes.CategoryNotFound, result.Error.Code);
    }

    [Fact]
    public async Task CategoryWriteConflictIsAdaptedAfterPrecheck()
    {
        var store = new CategoryPersistenceStub { WriteError = ErrorCodes.DuplicateCategoryName };
        var result = await new CreateCategoryCommandHandler(store, store).HandleAsync(new("Tools", null), default);
        Assert.Equal(ErrorCodes.DuplicateCategoryName, result.Error.Code);
        Assert.Equal(0, store.WriteCount);
    }

    [Fact]
    public async Task InvalidPaginationReturnsFailureBeforeReading()
    {
        var result = await new GetCategoriesQueryHandler(new CategoryPersistenceStub()).HandleAsync(new(1, 101), default);
        Assert.Equal(ErrorCodes.InvalidPagination, result.Error.Code);
        var queries = new InventoryQueriesStub();
        var history = await new GetProductInventoryMovementsQueryHandler(queries).HandleAsync(new(Guid.NewGuid(), Page: 0), default);
        Assert.Equal(ErrorCodes.InvalidPagination, history.Error.Code);
        Assert.Null(queries.LastFilter);
    }

    [Fact]
    public async Task InvalidHistoryDatesReturnFailureBeforeReading()
    {
        var queries = new InventoryQueriesStub();
        var now = DateTimeOffset.UtcNow;
        var result = await new GetProductInventoryMovementsQueryHandler(queries).HandleAsync(
            new(Guid.NewGuid(), StartDate: now, EndDate: now.AddDays(-1)), default);
        Assert.Equal(ErrorCodes.InvalidDateRange, result.Error.Code);
        Assert.Null(queries.LastFilter);
    }
}
