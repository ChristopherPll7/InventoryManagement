using InventoryManagement.Domain.Common;

namespace InventoryManagement.Domain.Categories;

public sealed class Category
{
    private Category() { }

    private Category(Guid id, string name, string? description)
    {
        Id = id;
        Name = TextValidation.Required(name, 150, "INVALID_CATEGORY_NAME", "Category name");
        Description = TextValidation.Optional(description, 500, "INVALID_CATEGORY_DESCRIPTION", "Category description");
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Category Create(string name, string? description) => new(Guid.NewGuid(), name, description);

    public void Update(string name, string? description)
    {
        var validName = TextValidation.Required(name, 150, "INVALID_CATEGORY_NAME", "Category name");
        var validDescription = TextValidation.Optional(description, 500, "INVALID_CATEGORY_DESCRIPTION", "Category description");
        Name = validName;
        Description = validDescription;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate(bool hasActiveProducts)
    {
        if (hasActiveProducts)
            throw new DomainException("CATEGORY_IN_USE", "The category contains active products.");
        if (!IsActive)
            return;
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void EnsureActive()
    {
        if (!IsActive)
            throw new DomainException("CATEGORY_INACTIVE", "The category is inactive.");
    }
}
