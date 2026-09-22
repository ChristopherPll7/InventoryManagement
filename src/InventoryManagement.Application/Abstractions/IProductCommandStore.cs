using InventoryManagement.Domain.Products;

namespace InventoryManagement.Application.Abstractions;

public interface IProductCommandStore
{
    Task CreateAsync(Product product, CancellationToken cancellationToken);
    Task<ProductDto> UpdateAsync(Product product, CancellationToken cancellationToken);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken);
}
