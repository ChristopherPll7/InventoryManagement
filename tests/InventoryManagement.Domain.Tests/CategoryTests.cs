using InventoryManagement.Domain.Categories;
using InventoryManagement.Domain.Common;

namespace InventoryManagement.Domain.Tests;

public sealed class CategoryTests
{
    [Fact]
    public void CreationNormalizesTextAndActivatesCategory()
    {
        var category = Category.Create(" Tools ", " Description ");
        Assert.Equal("Tools", category.Name);
        Assert.Equal("Description", category.Description);
        Assert.True(category.IsActive);
        Assert.NotEqual(Guid.Empty, category.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingNameIsRejected(string? name) =>
        Assert.Equal("INVALID_CATEGORY_NAME", Assert.Throws<DomainException>(() => Category.Create(name!, null)).Code);

    [Fact]
    public void UpdatePreservesIdentityAndCreationDate()
    {
        var category = Category.Create("Original", null);
        var id = category.Id;
        var createdAt = category.CreatedAt;
        category.Update(" Updated ", " Description ");
        Assert.Equal(id, category.Id);
        Assert.Equal(createdAt, category.CreatedAt);
        Assert.Equal("Updated", category.Name);
        Assert.NotNull(category.UpdatedAt);
    }

    [Fact]
    public void InvalidUpdateLeavesAllFieldsUnchanged()
    {
        var category = Category.Create("Original", "Original");
        Assert.Throws<DomainException>(() => category.Update("Changed", new string('x', 501)));
        Assert.Equal("Original", category.Name);
        Assert.Equal("Original", category.Description);
        Assert.Null(category.UpdatedAt);
    }

    [Fact]
    public void CategoryWithActiveProductsCannotBeDeactivated()
    {
        var category = Category.Create("Tools", null);
        Assert.Equal("CATEGORY_IN_USE", Assert.Throws<DomainException>(() => category.Deactivate(true)).Code);
        Assert.True(category.IsActive);
        Assert.Null(category.UpdatedAt);
    }

    [Fact]
    public void DeactivationIsIdempotentAndMetadataUpdateDoesNotReactivate()
    {
        var category = Category.Create("Tools", null);
        category.Deactivate(false);
        var updatedAt = category.UpdatedAt;
        category.Deactivate(false);
        Assert.Equal(updatedAt, category.UpdatedAt);
        category.Update("Updated", null);
        Assert.False(category.IsActive);
        Assert.Equal("CATEGORY_INACTIVE", Assert.Throws<DomainException>(category.EnsureActive).Code);
    }
}
