using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryAlert.UnitTests.Infrastructure.Repositories;

public class RecommendationTrendRepositoryTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetBySymbolAsync_ReturnsTrends_OrderedByPeriodDescending()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new RecommendationTrendRepository(context);

        var r1 = new RecommendationTrend { Id = 1, TickerSymbol = "AAPL", Period = "2026-01-01", StrongBuy = 10, Buy = 5, Hold = 2, Sell = 0, StrongSell = 0 };
        var r2 = new RecommendationTrend { Id = 2, TickerSymbol = "AAPL", Period = "2026-04-01", StrongBuy = 12, Buy = 6, Hold = 1, Sell = 0, StrongSell = 0 };

        context.RecommendationTrends.AddRange(r1, r2);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetBySymbolAsync("AAPL", CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Equal("2026-04-01", result.First().Period);
    }

    [Fact]
    public async Task UpsertRangeAsync_InsertsNewAndUpdatesExisting()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new RecommendationTrendRepository(context);

        var existing = new RecommendationTrend { Id = 1, TickerSymbol = "AAPL", Period = "2026-01-01", StrongBuy = 10, Buy = 5 };
        context.RecommendationTrends.Add(existing);
        await context.SaveChangesAsync();

        var updated = new RecommendationTrend { TickerSymbol = "AAPL", Period = "2026-01-01", StrongBuy = 15, Buy = 5 };
        var brandNew = new RecommendationTrend { TickerSymbol = "AAPL", Period = "2026-04-01", StrongBuy = 20, Buy = 5 };

        // Act
        await repo.UpsertRangeAsync(new[] { updated, brandNew }, CancellationToken.None);
        await context.SaveChangesAsync();

        // Assert
        var all = await repo.GetBySymbolAsync("AAPL", CancellationToken.None);
        Assert.Equal(2, all.Count());
        var updatedItem = all.First(x => x.Period == "2026-01-01");
        Assert.Equal(15, updatedItem.StrongBuy);
    }
}
