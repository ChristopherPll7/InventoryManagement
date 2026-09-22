using InventoryManagement.Domain.Categories;
using InventoryManagement.Domain.Inventory;
using InventoryManagement.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Persistence;

public sealed class InventoryReadDbContext(DbContextOptions<InventoryReadDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();

    public override int SaveChanges() => SaveChanges(true);

    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        throw new InvalidOperationException("This context is read-only. Use Dapper command stores for writes.");

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        SaveChangesAsync(true, cancellationToken);

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("This context is read-only. Use Dapper command stores for writes.");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().ToTable("Products").HasKey(x => x.Id);
        modelBuilder.Entity<Product>().Property<int>("CurrentStock");
        modelBuilder.Entity<Product>().Property(product => product.Price).HasPrecision(18, 2);
        modelBuilder.Entity<Category>().ToTable("Categories").HasKey(x => x.Id);
        modelBuilder.Entity<InventoryMovement>().ToTable("InventoryMovements").HasKey(x => x.Id);
    }
}
