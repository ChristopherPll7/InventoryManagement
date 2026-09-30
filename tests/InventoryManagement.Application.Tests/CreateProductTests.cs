using InventoryManagement.Application.Products;
using InventoryManagement.Domain.Common;
using InventoryManagement.Application.Abstractions;

namespace InventoryManagement.Application.Tests;

public sealed class CreateProductTests
{
    [Theory]
    [InlineData("DUPLICATE_PRODUCT_SKU")]
    [InlineData("CATEGORY_INACTIVE")]
    [InlineData("CATEGORY_NOT_FOUND")]
    public async Task WriteTimeBusinessConflictsBecomeFailures(string code)
    {
        var persistence = new ProductPersistenceStub { CreateError = new DomainException(code, "Write conflict.") };
        var result = await new CreateProductCommandHandler(persistence, persistence)
            .HandleAsync(new CreateProductCommand("Product", null, "SKU", 1, Guid.NewGuid()), default);
        Assert.True(result.IsFailure);
        Assert.Equal(new Error(code, "Write conflict."), result.Error);
        Assert.Null(persistence.CreatedProduct);
    }

    [Fact]
    public async Task TechnicalFailuresAreNotBusinessResults()
    {
        var failure = new InvalidOperationException("Connection unavailable.");
        var persistence = new ProductPersistenceStub { CreateError = failure };
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new CreateProductCommandHandler(persistence, persistence)
                .HandleAsync(new CreateProductCommand("Product", null, "SKU", 1, Guid.NewGuid()), default));
        Assert.Same(failure, exception);
    }

    [Fact]
    public async Task CancellationIsNotABusinessResult()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var persistence = new ProductPersistenceStub { CreateError = new OperationCanceledException(cancellation.Token) };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new CreateProductCommandHandler(persistence, persistence)
                .HandleAsync(new CreateProductCommand("Product", null, "SKU", 1, Guid.NewGuid()), cancellation.Token));
    }

    [Theory]
    [InlineData(null, "CATEGORY_NOT_FOUND")]
    [InlineData(false, "CATEGORY_INACTIVE")]
    public async Task UnavailableCategoryHasSpecificError(bool? active, string code)
    {
        var persistence = new ProductPersistenceStub { CategoryIsActive = active };
        var handler = new CreateProductCommandHandler(persistence, persistence);
        var command = new CreateProductCommand("Product", null, "SKU", 1, Guid.NewGuid());
        var result = await handler.HandleAsync(command, default);
        Assert.True(result.IsFailure);
        var error = result.Error;
        Assert.Equal(code, error.Code);
        Assert.Null(persistence.CreatedProduct);
    }

    [Fact]
    public async Task DuplicateSkuIsCheckedAfterNormalization()
    {
        var persistence = new ProductPersistenceStub { DuplicateSku = true };
        var handler = new CreateProductCommandHandler(persistence, persistence);
        var command = new CreateProductCommand("Product", null, " sku ", 1, Guid.NewGuid());
        var result = await handler.HandleAsync(command, default);
        Assert.True(result.IsFailure);
        var error = result.Error;
        Assert.Equal("DUPLICATE_PRODUCT_SKU", error.Code);
        Assert.Equal("SKU", persistence.CheckedSku);
        Assert.Null(persistence.CreatedProduct);
    }

    [Fact]
    public async Task InvalidInputIsRejectedBeforePersistenceAccess()
    {
        var persistence = new ProductPersistenceStub();
        var handler = new CreateProductCommandHandler(persistence, persistence);
        var command = new CreateProductCommand("Product", null, null!, 1, Guid.NewGuid());
        var result = await handler.HandleAsync(command, default);
        Assert.True(result.IsFailure);
        var error = result.Error;
        Assert.Equal("INVALID_PRODUCT_SKU", error.Code);
        Assert.Equal(0, persistence.ReadCount);
        Assert.Null(persistence.CreatedProduct);
    }

    [Fact]
    public async Task ValidProductIsPersistedAndIdentifierReturned()
    {
        var persistence = new ProductPersistenceStub();
        var handler = new CreateProductCommandHandler(persistence, persistence);
        var command = new CreateProductCommand(" Product ", null, " sku ", 1, Guid.NewGuid());
        var result = await handler.HandleAsync(command, default);
        Assert.True(result.IsSuccess);
        var id = result.Value;
        Assert.NotNull(persistence.CreatedProduct);
        Assert.Equal(id, persistence.CreatedProduct.Id);
        Assert.Equal("Product", persistence.CreatedProduct.Name);
        Assert.Equal("SKU", persistence.CreatedProduct.Sku);
    }
}
