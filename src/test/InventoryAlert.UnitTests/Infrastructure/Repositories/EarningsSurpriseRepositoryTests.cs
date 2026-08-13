using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryAlert.UnitTests.Infrastructure.Repositories;

public class EarningsSurpriseRepositoryTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetBySymbolAsync_ReturnsEarningsSurprises_OrderedByPeriodDescending()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new EarningsSurpriseRepository(context);

        var e1 = new EarningsSurprise { Id = 1, TickerSymbol = "AAPL", Period = new DateOnly(2026, 1, 1), ActualEps = 1.5, EstimateEps = 1.4 };
        var e2 = new EarningsSurprise { Id = 2, TickerSymbol = "AAPL", Period = new DateOnly(2026, 4, 1), ActualEps = 1.8, EstimateEps = 1.7 };

        context.EarningsSurprises.AddRange(e1, e2);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetBySymbolAsync("AAPL", CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Equal(new DateOnly(2026, 4, 1), result.First().Period);
    }

    [Fact]
    public async Task UpsertRangeAsync_InsertsNewAndUpdatesExisting()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new EarningsSurpriseRepository(context);

        var existing = new EarningsSurprise { Id = 1, TickerSymbol = "AAPL", Period = new DateOnly(2026, 1, 1), ActualEps = 1.5, EstimateEps = 1.4 };
        context.EarningsSurprises.Add(existing);
        await context.SaveChangesAsync();

        var updated = new EarningsSurprise { TickerSymbol = "AAPL", Period = new DateOnly(2026, 1, 1), ActualEps = 1.6, EstimateEps = 1.4 };
        var brandNew = new EarningsSurprise { TickerSymbol = "AAPL", Period = new DateOnly(2026, 4, 1), ActualEps = 1.8, EstimateEps = 1.7 };

        // Act
        await repo.UpsertRangeAsync(new[] { updated, brandNew }, CancellationToken.None);
        await context.SaveChangesAsync();

        // Assert
        var all = await repo.GetBySymbolAsync("AAPL", CancellationToken.None);
        Assert.Equal(2, all.Count());
        var updatedItem = all.First(x => x.Period == new DateOnly(2026, 1, 1));
        Assert.Equal(1.6, updatedItem.ActualEps);
    }
}
