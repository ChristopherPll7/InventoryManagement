namespace InventoryManagement.Application.Abstractions;

public interface IProductQueries
{
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<ProductDto>> GetAllAsync(PageRequest pagination, CancellationToken cancellationToken);
}
