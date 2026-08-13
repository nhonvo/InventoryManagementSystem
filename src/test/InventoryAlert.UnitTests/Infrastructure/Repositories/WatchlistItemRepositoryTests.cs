using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryAlert.UnitTests.Infrastructure.Repositories;

public class WatchlistItemRepositoryTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetByUserAndSymbolAsync_ReturnsItem_WhenExists()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new WatchlistItemRepository(context);
        var userId = Guid.NewGuid();
        var item = new WatchlistItem { UserId = userId, TickerSymbol = "AAPL", CreatedAt = DateTime.UtcNow };

        context.WatchlistItems.Add(item);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetByUserAndSymbolAsync(userId.ToString(), "AAPL", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("AAPL", result.TickerSymbol);
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsAllUserItems()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new WatchlistItemRepository(context);
        var userId = Guid.NewGuid();

        context.WatchlistItems.AddRange(
            new WatchlistItem { UserId = userId, TickerSymbol = "AAPL", CreatedAt = DateTime.UtcNow },
            new WatchlistItem { UserId = userId, TickerSymbol = "MSFT", CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        // Act
        var items = await repo.GetByUserIdAsync(userId.ToString(), CancellationToken.None);

        // Assert
        Assert.Equal(2, items.Count());
    }
}
