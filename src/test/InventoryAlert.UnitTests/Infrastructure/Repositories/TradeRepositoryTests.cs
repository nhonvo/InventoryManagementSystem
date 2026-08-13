using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryAlert.UnitTests.Infrastructure.Repositories;

public class TradeRepositoryTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetByUserAndSymbolAsync_ReturnsTradesForUserAndSymbol()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new TradeRepository(context);
        var userId = Guid.NewGuid();
        var trade1 = new Trade { Id = Guid.NewGuid(), UserId = userId, TickerSymbol = "AAPL", Type = TradeType.Buy, Quantity = 10, UnitPrice = 150m, TradedAt = DateTime.UtcNow };
        var trade2 = new Trade { Id = Guid.NewGuid(), UserId = userId, TickerSymbol = "GOOGL", Type = TradeType.Buy, Quantity = 5, UnitPrice = 2800m, TradedAt = DateTime.UtcNow };

        context.Trades.AddRange(trade1, trade2);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetByUserAndSymbolAsync(userId, "AAPL", CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("AAPL", result.First().TickerSymbol);
    }

    [Fact]
    public async Task GetNetHoldingsAsync_CalculatesBuyMinusSellQuantityCorrectly()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new TradeRepository(context);
        var userId = Guid.NewGuid();
        var buy = new Trade { Id = Guid.NewGuid(), UserId = userId, TickerSymbol = "AAPL", Type = TradeType.Buy, Quantity = 100, UnitPrice = 150m, TradedAt = DateTime.UtcNow.AddDays(-2) };
        var sell = new Trade { Id = Guid.NewGuid(), UserId = userId, TickerSymbol = "AAPL", Type = TradeType.Sell, Quantity = 30, UnitPrice = 160m, TradedAt = DateTime.UtcNow };

        context.Trades.AddRange(buy, sell);
        await context.SaveChangesAsync();

        // Act
        var netHoldings = await repo.GetNetHoldingsAsync(userId, "AAPL", CancellationToken.None);

        // Assert
        Assert.Equal(70m, netHoldings);
    }

    [Fact]
    public async Task GetTradedSymbolsPagedAsync_FiltersAndPagesDistinctSymbols()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new TradeRepository(context);
        var userId = Guid.NewGuid();
        context.Trades.AddRange(
            new Trade { Id = Guid.NewGuid(), UserId = userId, TickerSymbol = "AAPL", Type = TradeType.Buy, Quantity = 10, UnitPrice = 150m, TradedAt = DateTime.UtcNow },
            new Trade { Id = Guid.NewGuid(), UserId = userId, TickerSymbol = "MSFT", Type = TradeType.Buy, Quantity = 5, UnitPrice = 300m, TradedAt = DateTime.UtcNow },
            new Trade { Id = Guid.NewGuid(), UserId = userId, TickerSymbol = "AMZN", Type = TradeType.Buy, Quantity = 2, UnitPrice = 3300m, TradedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        // Act
        var (symbols, totalCount) = await repo.GetTradedSymbolsPagedAsync(userId, 1, 2, "A", CancellationToken.None);

        // Assert
        Assert.Equal(2, totalCount);
        Assert.Contains("AAPL", symbols);
        Assert.Contains("AMZN", symbols);
    }

    [Fact]
    public async Task GetByUserAndSymbolsAsync_ReturnsTradesForMultipleSymbols()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new TradeRepository(context);
        var userId = Guid.NewGuid();
        context.Trades.AddRange(
            new Trade { Id = Guid.NewGuid(), UserId = userId, TickerSymbol = "AAPL", Type = TradeType.Buy, Quantity = 10, UnitPrice = 150m, TradedAt = DateTime.UtcNow },
            new Trade { Id = Guid.NewGuid(), UserId = userId, TickerSymbol = "MSFT", Type = TradeType.Buy, Quantity = 5, UnitPrice = 300m, TradedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        // Act
        var trades = await repo.GetByUserAndSymbolsAsync(userId, new[] { "AAPL", "MSFT" }, CancellationToken.None);

        // Assert
        Assert.Equal(2, trades.Count());
    }
}
