using InventoryManagement.Domain.Products;

namespace InventoryManagement.Application.Abstractions;

public interface IProductCommandStore
{
    Task CreateAsync(Product product, CancellationToken cancellationToken);
    Task<ProductDto> UpdateAsync(Guid id, ProductDetails details, CancellationToken cancellationToken);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken);
}
