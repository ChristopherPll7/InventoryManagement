using InventoryManagement.Application.Abstractions;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<InventoryReadDbContext>(options => options.UseSqlServer(connectionString));
        services.AddSingleton(new SqlConnectionFactory(connectionString));
        services.AddScoped<IProductCommandStore, ProductCommandStore>();
        services.AddScoped<IProductValidationQueries, ProductValidationQueries>();
        services.AddScoped<IInventoryCommandStore, InventoryCommandStore>();
        services.AddScoped<IInventoryQueries, InventoryQueries>();
        services.AddScoped<IProductQueries, ProductQueries>();
        services.AddScoped<ICategoryCommandStore, CategoryCommandStore>();
        services.AddScoped<ICategoryQueries, CategoryQueries>();
        return services;
    }
}
