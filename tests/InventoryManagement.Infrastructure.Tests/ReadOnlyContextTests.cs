using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Tests;

public sealed class ReadOnlyContextTests
{
    [Fact]
    public void ReadContextRejectsSynchronousWrites()
    {
        using var context = CreateContext();
        Assert.Throws<InvalidOperationException>(() => context.SaveChanges());
        Assert.Throws<InvalidOperationException>(() => context.SaveChanges(false));
    }

    [Fact]
    public async Task ReadContextRejectsAsynchronousWrites()
    {
        await using var context = CreateContext();
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync(false));
    }

    private static InventoryReadDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<InventoryReadDbContext>().UseSqlServer("Server=unused;Database=unused").Options);
}
