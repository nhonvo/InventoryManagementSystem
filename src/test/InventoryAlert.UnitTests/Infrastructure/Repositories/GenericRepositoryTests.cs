using FluentAssertions;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryAlert.UnitTests.Infrastructure.Repositories;

public class GenericRepositoryTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task AddRangeAsync_AddsMultipleEntities()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new GenericRepository<StockListing>(context);
        var listings = new List<StockListing>
        {
            new() { TickerSymbol = "AAPL", Name = "Apple" },
            new() { TickerSymbol = "MSFT", Name = "Microsoft" }
        };

        // Act
        await repo.AddRangeAsync(listings, CancellationToken.None);
        await context.SaveChangesAsync();

        // Assert
        var all = await repo.GetAllAsync(CancellationToken.None);
        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesDetachedEntity()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new GenericRepository<StockListing>(context);
        var listing = new StockListing { TickerSymbol = "AAPL", Name = "Apple" };
        await repo.AddAsync(listing, CancellationToken.None);
        await context.SaveChangesAsync();

        // Detach
        context.Entry(listing).State = EntityState.Detached;

        // Act
        listing.Name = "Apple Inc.";
        await repo.UpdateAsync(listing, CancellationToken.None);
        await context.SaveChangesAsync();

        // Assert
        var updated = await repo.GetByIdAsync(listing.Id, CancellationToken.None);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Apple Inc.");
    }
}
