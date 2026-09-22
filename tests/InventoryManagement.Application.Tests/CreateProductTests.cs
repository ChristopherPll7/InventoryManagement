using InventoryManagement.Application.Products;
using InventoryManagement.Domain.Common;

namespace InventoryManagement.Application.Tests;

public sealed class CreateProductTests
{
    [Theory]
    [InlineData(null, "CATEGORY_NOT_FOUND")]
    [InlineData(false, "CATEGORY_INACTIVE")]
    public async Task UnavailableCategoryHasSpecificError(bool? active, string code)
    {
        var persistence = new ProductPersistenceStub { CategoryIsActive = active };
        var handler = new CreateProductCommandHandler(persistence, persistence);
        var command = new CreateProductCommand("Product", null, "SKU", 1, Guid.NewGuid());
        var error = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(command, default));
        Assert.Equal(code, error.Code);
        Assert.Null(persistence.CreatedProduct);
    }

    [Fact]
    public async Task DuplicateSkuIsCheckedAfterNormalization()
    {
        var persistence = new ProductPersistenceStub { DuplicateSku = true };
        var handler = new CreateProductCommandHandler(persistence, persistence);
        var command = new CreateProductCommand("Product", null, " sku ", 1, Guid.NewGuid());
        var error = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(command, default));
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
        var error = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(command, default));
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
        var id = await handler.HandleAsync(command, default);
        Assert.NotNull(persistence.CreatedProduct);
        Assert.Equal(id, persistence.CreatedProduct.Id);
        Assert.Equal("Product", persistence.CreatedProduct.Name);
        Assert.Equal("SKU", persistence.CreatedProduct.Sku);
    }
}
