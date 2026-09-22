using InventoryManagement.Application.Categories;
using InventoryManagement.Application.Products;
using InventoryManagement.Domain.Categories;
using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Products;

namespace InventoryManagement.Application.Tests;

public sealed class CatalogCommandTests
{
    [Fact]
    public async Task CategoryCreateValidatesAndReturnsPersistedIdentifier()
    {
        var store = new CategoryPersistenceStub();
        var id = await new CreateCategoryCommandHandler(store, store).HandleAsync(new CreateCategoryCommand(" Tools ", null), default);
        Assert.Equal(id, store.Category!.Id);
        Assert.Equal("Tools", store.Category.Name);
        Assert.Equal(1, store.WriteCount);
    }

    [Fact]
    public async Task CategoryDuplicateIsRejectedBeforeWriting()
    {
        var store = new CategoryPersistenceStub { DuplicateName = true };
        var error = await Assert.ThrowsAsync<DomainException>(() =>
            new CreateCategoryCommandHandler(store, store).HandleAsync(new CreateCategoryCommand("Tools", null), default));
        Assert.Equal("DUPLICATE_CATEGORY_NAME", error.Code);
        Assert.Equal(0, store.WriteCount);
    }

    [Fact]
    public async Task CategoryUpdateExcludesItsOwnName()
    {
        var store = new CategoryPersistenceStub();
        var category = Category.Create("Tools", null);
        await store.CreateAsync(category, default);
        var result = await new UpdateCategoryCommandHandler(store, store).HandleAsync(new UpdateCategoryCommand(category.Id, "Tools", "Updated"), default);
        Assert.Equal(category.Id, store.ExcludedCategoryId);
        Assert.Equal("Updated", result.Description);
    }

    [Fact]
    public async Task MissingCategoryUpdateIsNotFound()
    {
        var store = new CategoryPersistenceStub();
        var error = await Assert.ThrowsAsync<DomainException>(() =>
            new UpdateCategoryCommandHandler(store, store).HandleAsync(new UpdateCategoryCommand(Guid.NewGuid(), "Tools", null), default));
        Assert.Equal("CATEGORY_NOT_FOUND", error.Code);
        Assert.Equal(0, store.WriteCount);
    }

    [Fact]
    public async Task ProductUpdateNormalizesSkuAndExcludesItself()
    {
        var product = Product.Create("Product", null, "SKU", 1, Guid.NewGuid());
        var store = new ProductPersistenceStub { ExistingProduct = product };
        var result = await new UpdateProductCommandHandler(store, store).HandleAsync(
            new UpdateProductCommand(product.Id, " Updated ", "Details", " sku ", 2, product.CategoryId), default);
        Assert.Equal(product.Id, store.ExcludedProductId);
        Assert.Equal("SKU", store.CheckedSku);
        Assert.Equal("Updated", result.Name);
        Assert.Equal(2, result.Price);
        Assert.NotNull(store.UpdatedProduct);
    }

    [Theory]
    [InlineData(null, "CATEGORY_NOT_FOUND")]
    [InlineData(false, "CATEGORY_INACTIVE")]
    public async Task ProductUpdateRequiresExistingActiveCategory(bool? categoryIsActive, string code)
    {
        var product = Product.Create("Product", null, "SKU", 1, Guid.NewGuid());
        var store = new ProductPersistenceStub { ExistingProduct = product, CategoryIsActive = categoryIsActive };
        var error = await Assert.ThrowsAsync<DomainException>(() => new UpdateProductCommandHandler(store, store).HandleAsync(
            new UpdateProductCommand(product.Id, "Updated", null, "SKU", 2, Guid.NewGuid()), default));
        Assert.Equal(code, error.Code);
        Assert.Null(store.UpdatedProduct);
    }

    [Fact]
    public async Task ProductUpdateRejectsDuplicateSku()
    {
        var product = Product.Create("Product", null, "SKU", 1, Guid.NewGuid());
        var store = new ProductPersistenceStub { ExistingProduct = product, DuplicateSku = true };
        var error = await Assert.ThrowsAsync<DomainException>(() => new UpdateProductCommandHandler(store, store).HandleAsync(
            new UpdateProductCommand(product.Id, "Updated", null, "DUPLICATE", 2, product.CategoryId), default));
        Assert.Equal("DUPLICATE_PRODUCT_SKU", error.Code);
        Assert.Null(store.UpdatedProduct);
    }

    [Fact]
    public async Task MissingProductUpdateIsNotFound()
    {
        var store = new ProductPersistenceStub();
        var error = await Assert.ThrowsAsync<DomainException>(() => new UpdateProductCommandHandler(store, store).HandleAsync(
            new UpdateProductCommand(Guid.NewGuid(), "Updated", null, "SKU", 2, Guid.NewGuid()), default));
        Assert.Equal("PRODUCT_NOT_FOUND", error.Code);
        Assert.Null(store.UpdatedProduct);
    }

    [Fact]
    public async Task DeleteProductDeactivatesWithoutChangingIdentity()
    {
        var product = Product.Create("Product", null, "SKU", 1, Guid.NewGuid());
        var store = new ProductPersistenceStub { ExistingProduct = product };
        await new DeleteProductCommandHandler(store).HandleAsync(new DeleteProductCommand(product.Id), default);
        Assert.False(product.IsActive);
        Assert.Equal("SKU", product.Sku);
    }

    [Fact]
    public async Task DeleteCategoryPropagatesActiveProductConflict()
    {
        var store = new CategoryPersistenceStub { HasActiveProducts = true };
        var category = Category.Create("Tools", null);
        await store.CreateAsync(category, default);
        var error = await Assert.ThrowsAsync<DomainException>(() =>
            new DeleteCategoryCommandHandler(store).HandleAsync(new DeleteCategoryCommand(category.Id), default));
        Assert.Equal("CATEGORY_IN_USE", error.Code);
        Assert.True(category.IsActive);
    }
}
