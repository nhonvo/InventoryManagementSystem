using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryAlert.UnitTests.Infrastructure.Repositories;

public class StockListingRepositoryTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task FindBySymbolAsync_ReturnsStockListing_WhenSymbolExists()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new StockListingRepository(context);
        var stock = new StockListing { Id = 1, TickerSymbol = "AAPL", Name = "Apple Inc.", Exchange = "NASDAQ", Currency = "USD" };
        context.StockListings.Add(stock);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.FindBySymbolAsync("AAPL", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Apple Inc.", result.Name);
    }

    [Fact]
    public async Task FindBySymbolsAsync_ReturnsListings_ForGivenSymbols()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new StockListingRepository(context);
        context.StockListings.AddRange(
            new StockListing { Id = 1, TickerSymbol = "AAPL", Name = "Apple Inc.", Exchange = "NASDAQ", Currency = "USD" },
            new StockListing { Id = 2, TickerSymbol = "MSFT", Name = "Microsoft Corp.", Exchange = "NASDAQ", Currency = "USD" }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await repo.FindBySymbolsAsync(new[] { "AAPL", "MSFT" }, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetActiveSymbolsAsync_CombinesWatchlistTradesAndAlerts()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new StockListingRepository(context);
        var userId = Guid.NewGuid();

        context.WatchlistItems.Add(new WatchlistItem { UserId = userId, TickerSymbol = "AAPL", CreatedAt = DateTime.UtcNow });
        context.Trades.Add(new Trade { Id = Guid.NewGuid(), UserId = userId, TickerSymbol = "GOOGL", Type = TradeType.Buy, Quantity = 5, UnitPrice = 2800m, TradedAt = DateTime.UtcNow });
        context.AlertRules.Add(new AlertRule { Id = Guid.NewGuid(), UserId = userId, TickerSymbol = "MSFT", Condition = AlertCondition.PriceAbove, TargetValue = 300m, IsActive = true });

        await context.SaveChangesAsync();

        // Act
        var symbols = await repo.GetActiveSymbolsAsync(CancellationToken.None);

        // Assert
        Assert.Equal(3, symbols.Count());
        Assert.Contains("AAPL", symbols);
        Assert.Contains("GOOGL", symbols);
        Assert.Contains("MSFT", symbols);
    }

    [Fact]
    public async Task SearchAsync_ReturnsMatchingListings()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new StockListingRepository(context);
        context.StockListings.Add(new StockListing { Id = 1, TickerSymbol = "AAPL", Name = "Apple Inc." });
        await context.SaveChangesAsync();

        // Act
        var result = await repo.SearchAsync("Apple", CancellationToken.None);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task SearchAsync_ReturnsAll_WhenQueryEmpty()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new StockListingRepository(context);
        context.StockListings.Add(new StockListing { Id = 1, TickerSymbol = "AAPL", Name = "Apple Inc." });
        await context.SaveChangesAsync();

        // Act
        var result = await repo.SearchAsync("", CancellationToken.None);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task GetActiveSymbolsAsync_ReturnsAllStockListingSymbols_WhenNoUserActivity()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new StockListingRepository(context);
        context.StockListings.Add(new StockListing { Id = 1, TickerSymbol = "AAPL", Name = "Apple Inc." });
        await context.SaveChangesAsync();

        // Act
        var symbols = await repo.GetActiveSymbolsAsync(CancellationToken.None);

        // Assert
        Assert.Single(symbols);
        Assert.Contains("AAPL", symbols);
    }
}
