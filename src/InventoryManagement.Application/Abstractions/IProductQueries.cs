namespace InventoryManagement.Application.Abstractions;

public interface IProductQueries
{
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ProductDto>> GetAllAsync(CancellationToken cancellationToken);
}
