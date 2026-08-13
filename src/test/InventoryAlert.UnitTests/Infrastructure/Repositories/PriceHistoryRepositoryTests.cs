using FluentAssertions;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryAlert.UnitTests.Infrastructure.Repositories;

public class PriceHistoryRepositoryTests
{
    private static DbContextOptions<AppDbContext> CreateOptions()
        => new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    [Fact]
    public async Task GetBySymbolAsync_ReturnsOrderedPriceHistories()
    {
        // Arrange
        var options = CreateOptions();
        await using var context = new AppDbContext(options);
        var repo = new PriceHistoryRepository(context);

        context.PriceHistories.AddRange(
            new PriceHistory { TickerSymbol = "AAPL", Price = 150m, RecordedAt = DateTime.UtcNow.AddHours(-2) },
            new PriceHistory { TickerSymbol = "AAPL", Price = 155m, RecordedAt = DateTime.UtcNow.AddHours(-1) },
            new PriceHistory { TickerSymbol = "MSFT", Price = 300m, RecordedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetBySymbolAsync("AAPL", 10, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.First().Price.Should().Be(155m);
    }

    [Fact]
    public async Task DeleteOlderThanAsync_RemovesRecordsOlderThanCutoff()
    {
        // Arrange
        var options = CreateOptions();
        await using var context = new AppDbContext(options);
        var repo = new PriceHistoryRepository(context);

        var oldRecord = new PriceHistory { TickerSymbol = "AAPL", Price = 100m, RecordedAt = DateTime.UtcNow.AddDays(-30) };
        var newRecord = new PriceHistory { TickerSymbol = "AAPL", Price = 150m, RecordedAt = DateTime.UtcNow };
        context.PriceHistories.AddRange(oldRecord, newRecord);
        await context.SaveChangesAsync();

        // Act
        await repo.DeleteOlderThanAsync(DateTime.UtcNow.AddDays(-7), CancellationToken.None);

        // Assert
        var remaining = await context.PriceHistories.ToListAsync();
        remaining.Should().HaveCount(1);
        remaining.First().Price.Should().Be(150m);
    }
}
