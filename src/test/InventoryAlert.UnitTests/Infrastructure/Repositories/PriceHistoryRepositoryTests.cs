using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryAlert.UnitTests.Infrastructure.Repositories;

public class PriceHistoryRepositoryTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetBySymbolAsync_ReturnsPricesOrderedByRecordedAtDescending()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new PriceHistoryRepository(context);

        var p1 = new PriceHistory { Id = 1L, TickerSymbol = "AAPL", Price = 150m, RecordedAt = DateTime.UtcNow.AddHours(-2) };
        var p2 = new PriceHistory { Id = 2L, TickerSymbol = "AAPL", Price = 155m, RecordedAt = DateTime.UtcNow };

        context.PriceHistories.AddRange(p1, p2);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetBySymbolAsync("AAPL", 10, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Equal(155m, result.First().Price);
    }
}
