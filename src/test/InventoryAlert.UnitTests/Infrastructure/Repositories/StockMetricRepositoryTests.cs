using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryAlert.UnitTests.Infrastructure.Repositories;

public class StockMetricRepositoryTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetBySymbolAsync_ReturnsStockMetric_WhenFound()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new StockMetricRepository(context);
        var metric = new StockMetric { TickerSymbol = "AAPL", PeRatio = 25.5, LastSyncedAt = DateTime.UtcNow };

        context.StockMetrics.Add(metric);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetBySymbolAsync("AAPL", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(25.5, result.PeRatio);
    }

    [Fact]
    public async Task UpsertAsync_AddsNewMetric_WhenNotExists()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new StockMetricRepository(context);
        var metric = new StockMetric { TickerSymbol = "MSFT", PeRatio = 30.0, LastSyncedAt = DateTime.UtcNow };

        // Act
        await repo.UpsertAsync(metric, CancellationToken.None);
        await context.SaveChangesAsync();

        // Assert
        var found = await context.StockMetrics.FindAsync("MSFT");
        Assert.NotNull(found);
        Assert.Equal(30.0, found.PeRatio);
    }

    [Fact]
    public async Task UpsertAsync_UpdatesExistingMetric_WhenExists()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new StockMetricRepository(context);
        context.StockMetrics.Add(new StockMetric { TickerSymbol = "AAPL", PeRatio = 20.0, LastSyncedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var updated = new StockMetric { TickerSymbol = "AAPL", PeRatio = 28.0, LastSyncedAt = DateTime.UtcNow };

        // Act
        await repo.UpsertAsync(updated, CancellationToken.None);
        await context.SaveChangesAsync();

        // Assert
        var found = await context.StockMetrics.FindAsync("AAPL");
        Assert.NotNull(found);
        Assert.Equal(28.0, found.PeRatio);
    }
}
