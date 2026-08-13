using FluentAssertions;
using InventoryAlert.Api.Services;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Application.Services;

public class PortfolioServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IStockDataService> _stockData = new();
    private readonly Mock<ILogger<PortfolioService>> _logger = new();
    private readonly PortfolioService _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;
    private const string UserId = "00000000-0000-0000-0000-000000000001";
    private static readonly Guid UserGuid = Guid.Parse(UserId);

    public PortfolioServiceTests()
    {
        _sut = new PortfolioService(_uow.Object, _stockData.Object, _logger.Object);

        // Mock ExecuteSynchronizedAsync for various return types
        _uow.Setup(u => u.ExecuteSynchronizedAsync(It.IsAny<Func<Task<IEnumerable<Trade>>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<IEnumerable<Trade>>>, CancellationToken>((func, _) => func());

        _uow.Setup(u => u.ExecuteSynchronizedAsync(It.IsAny<Func<Task<StockListing?>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<StockListing?>>, CancellationToken>((func, _) => func());

        _uow.Setup(u => u.ExecuteSynchronizedAsync(It.IsAny<Func<Task<IEnumerable<AlertRule>>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<IEnumerable<AlertRule>>>, CancellationToken>((func, _) => func());

        _uow.Setup(u => u.ExecuteSynchronizedAsync(It.IsAny<Func<Task<WatchlistItem?>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<WatchlistItem?>>, CancellationToken>((func, _) => func());

        // Standard mock to invoke the transaction delegate
        _uow.Setup(u => u.ExecuteTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((action, _) => action());
    }

    [Fact]
    public async Task GetPosition_CalculatesHoldingsCorrectly_FromMultipleTrades()
    {
        // Arrange
        var symbol = "AAPL";
        var listing = new StockListing { Id = 1, TickerSymbol = symbol, Name = "Apple" };
        var trades = (IEnumerable<Trade>)new List<Trade>
        {
            new() { Type = TradeType.Buy, Quantity = 10, UnitPrice = 150m, TickerSymbol = symbol },
            new() { Type = TradeType.Buy, Quantity = 5, UnitPrice = 160m, TickerSymbol = symbol },
            new() { Type = TradeType.Sell, Quantity = 3, UnitPrice = 170m, TickerSymbol = symbol }
        };

        _uow.Setup(u => u.Trades.GetByUserAndSymbolAsync(UserGuid, symbol, Ct)).ReturnsAsync(trades);
        _uow.Setup(u => u.StockListings.FindBySymbolAsync(symbol, Ct)).ReturnsAsync(listing);
        _stockData.Setup(s => s.GetQuoteAsync(symbol, Ct))
            .ReturnsAsync(new StockQuoteResponse(symbol, 180m, 2m, 1.1, 182m, 178m, 179m, 178m, DateTime.UtcNow));

        // Act
        var result = await _sut.GetPositionBySymbolAsync(symbol, UserId, Ct);

        // Assert
        result.Should().NotBeNull();
        result!.HoldingsCount.Should().Be(12); // (10 + 5) - 3
        result.AveragePrice.Should().Be(153.33333333333333333333333333m); // (10*150 + 5*160) / 15
        result.CurrentPrice.Should().Be(180m);
        result.MarketValue.Should().Be(12 * 180m);
        result.TotalReturn.Should().Be((12 * 180m) - (12 * 153.33333333333333333333333333m));
    }

    [Fact]
    public async Task RecordTrade_Throws_WhenSellingMoreThanOwned()
    {
        // Arrange
        var symbol = "MSFT";
        var request = new TradeRequest(TradeType.Sell, 50, 400m, "Selling too much");

        _uow.Setup(u => u.Trades.GetNetHoldingsAsync(UserGuid, symbol, Ct)).ReturnsAsync(10m);

        // Act
        var act = () => _sut.RecordTradeAsync(symbol, request, UserId, Ct);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Insufficient holdings*");
    }

    [Fact]
    public async Task OpenPosition_EnsuresListingExists_BeforeAdding()
    {
        // Arrange
        var request = new CreatePositionRequest("INVALID", 10, 100m, null);
        _uow.Setup(u => u.StockListings.FindBySymbolAsync("INVALID", Ct)).ReturnsAsync((StockListing?)null);

        // Act
        var act = () => _sut.OpenPositionAsync(request, UserId, Ct);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*must be resolved before opening a position.*");
    }

    [Fact]
    public async Task RemovePosition_Throws_IfActiveAlertsExist()
    {
        // Arrange
        var symbol = "TSLA";
        var activeRules = (IEnumerable<AlertRule>)new List<AlertRule> {
            new() { TickerSymbol = symbol, IsActive = true }
        };
        _uow.Setup(u => u.AlertRules.GetByUserIdAsync(UserId, Ct)).ReturnsAsync(activeRules);

        // Act
        var act = () => _sut.RemovePositionAsync(symbol, UserId, Ct);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot remove a position with active alert rules.*");
    }

    [Fact]
    public async Task GetPositionsPagedAsync_ReturnsOnlyTradedSymbols()
    {
        // Arrange
        var query = new PortfolioQueryParams { PageNumber = 1, PageSize = 10 };
        var tradedSymbols = (IEnumerable<string>)new List<string> { "AAPL" };
        var listing = new StockListing { Id = 1, TickerSymbol = "AAPL", Name = "Apple Inc" };
        var trades = (IEnumerable<Trade>)new List<Trade>
        {
            new() { Type = TradeType.Buy, Quantity = 5, UnitPrice = 150m, TickerSymbol = "AAPL" }
        };

        _uow.Setup(u => u.Trades.GetTradedSymbolsPagedAsync(UserGuid, 1, 10, null, Ct))
            .ReturnsAsync((tradedSymbols, 1));
        _uow.Setup(u => u.ExecuteSynchronizedAsync(It.IsAny<Func<Task<IEnumerable<Trade>>>>(), Ct))
            .ReturnsAsync(trades);
        _uow.Setup(u => u.ExecuteSynchronizedAsync(It.IsAny<Func<Task<IEnumerable<StockListing>>>>(), Ct))
            .ReturnsAsync(new List<StockListing> { listing });
        _stockData.Setup(s => s.GetQuoteAsync("AAPL", Ct))
            .ReturnsAsync(new StockQuoteResponse("AAPL", 170m, 2m, 1.2, 172m, 168m, 169m, 168m, DateTime.UtcNow));

        // Act
        var result = await _sut.GetPositionsPagedAsync(query, UserId, Ct);

        // Assert
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items.First().Symbol.Should().Be("AAPL");
        result.Items.First().HoldingsCount.Should().Be(5);
    }

    [Fact]
    public async Task GetPortfolioAlertsAsync_ReturnsBreachedAlerts()
    {
        // Arrange
        var rules = (IEnumerable<AlertRule>)new List<AlertRule>
        {
            new() { TickerSymbol = "AAPL", Condition = AlertCondition.PriceAbove, TargetValue = 150m, IsActive = true }
        };
        _uow.Setup(u => u.ExecuteSynchronizedAsync(It.IsAny<Func<Task<IEnumerable<AlertRule>>>>(), Ct))
            .ReturnsAsync(rules);
        _stockData.Setup(s => s.GetQuoteAsync("AAPL", Ct))
            .ReturnsAsync(new StockQuoteResponse("AAPL", 160m, 2m, 1.2, 162m, 158m, 159m, 158m, DateTime.UtcNow));

        // Act
        var alerts = await _sut.GetPortfolioAlertsAsync(UserId, Ct);

        // Assert
        alerts.Should().HaveCount(1);
        alerts.First().Symbol.Should().Be("AAPL");
        alerts.First().CurrentPrice.Should().Be(160m);
    }

    [Fact]
    public async Task GetTradesBySymbolAsync_ReturnsMappedTradeResponses()
    {
        // Arrange
        var trades = (IEnumerable<Trade>)new List<Trade>
        {
            new() { Id = Guid.NewGuid(), TickerSymbol = "AAPL", Type = TradeType.Buy, Quantity = 10, UnitPrice = 150m, TradedAt = DateTime.UtcNow }
        };
        _uow.Setup(u => u.Trades.GetByUserAndSymbolAsync(UserGuid, "AAPL", Ct)).ReturnsAsync(trades);

        // Act
        var result = await _sut.GetTradesBySymbolAsync("AAPL", UserId, Ct);

        // Assert
        result.Should().HaveCount(1);
        result.First().Symbol.Should().Be("AAPL");
        result.First().Quantity.Should().Be(10);
    }

    [Fact]
    public async Task RemovePositionAsync_DeletesTradesAndWatchlistItem_WhenNoActiveAlerts()
    {
        // Arrange
        _uow.Setup(u => u.AlertRules.GetByUserIdAsync(UserId, Ct)).ReturnsAsync(new List<AlertRule>());
        var watchItem = new WatchlistItem { UserId = UserGuid, TickerSymbol = "AAPL" };
        _uow.Setup(u => u.WatchlistItems.GetByUserAndSymbolAsync(UserId, "AAPL", Ct)).ReturnsAsync(watchItem);
        var trades = (IEnumerable<Trade>)new List<Trade> { new() { Id = Guid.NewGuid(), UserId = UserGuid, TickerSymbol = "AAPL" } };
        _uow.Setup(u => u.Trades.GetByUserAndSymbolAsync(UserGuid, "AAPL", Ct)).ReturnsAsync(trades);

        // Act
        await _sut.RemovePositionAsync("AAPL", UserId, Ct);

        // Assert
        _uow.Verify(u => u.WatchlistItems.DeleteAsync(watchItem, Ct), Times.Once);
        _uow.Verify(u => u.Trades.DeleteAsync(It.IsAny<Trade>(), Ct), Times.Once);
    }

    [Fact]
    public async Task BulkImportPositionsAsync_InvokesOpenPositionForRequests()
    {
        // Arrange
        var requests = new List<CreatePositionRequest>
        {
            new("INVALID", 10, 100m, null)
        };

        // Act
        await _sut.BulkImportPositionsAsync(requests, UserId, Ct);

        // Assert
        _uow.Verify(u => u.ExecuteTransactionAsync(It.IsAny<Func<Task>>(), Ct), Times.Once);
    }

    [Fact]
    public async Task GetPositionBySymbolAsync_ReturnsNull_WhenNoTradesOrNoListing()
    {
        // Arrange
        _uow.Setup(u => u.Trades.GetByUserAndSymbolAsync(UserGuid, "NOTRADES", Ct)).ReturnsAsync(new List<Trade>());

        // Act
        var res1 = await _sut.GetPositionBySymbolAsync("NOTRADES", UserId, Ct);

        // Assert
        res1.Should().BeNull();
    }

    [Fact]
    public async Task GetPortfolioAlertsAsync_HandlesPriceBelowAndUnbreached()
    {
        // Arrange
        var rules = (IEnumerable<AlertRule>)new List<AlertRule>
        {
            new() { TickerSymbol = "AAPL", Condition = AlertCondition.PriceBelow, TargetValue = 200m, IsActive = true },
            new() { TickerSymbol = "MSFT", Condition = AlertCondition.PriceAbove, TargetValue = 500m, IsActive = true },
            new() { TickerSymbol = "NOQUOTE", Condition = AlertCondition.PriceAbove, TargetValue = 100m, IsActive = true }
        };
        _uow.Setup(u => u.ExecuteSynchronizedAsync(It.IsAny<Func<Task<IEnumerable<AlertRule>>>>(), Ct)).ReturnsAsync(rules);
        _stockData.Setup(s => s.GetQuoteAsync("AAPL", Ct))
            .ReturnsAsync(new StockQuoteResponse("AAPL", 150m, 0m, 0, 0m, 0m, 0m, 0m, DateTime.UtcNow));
        _stockData.Setup(s => s.GetQuoteAsync("MSFT", Ct))
            .ReturnsAsync(new StockQuoteResponse("MSFT", 400m, 0m, 0, 0m, 0m, 0m, 0m, DateTime.UtcNow));
        _stockData.Setup(s => s.GetQuoteAsync("NOQUOTE", Ct)).ReturnsAsync((StockQuoteResponse?)null);

        // Act
        var alerts = await _sut.GetPortfolioAlertsAsync(UserId, Ct);

        // Assert
        alerts.Should().HaveCount(1);
        alerts.First().Symbol.Should().Be("AAPL");
    }

    [Fact]
    public async Task OpenPositionAsync_SuccessfullyOpensPosition_WhenProfileResolved()
    {
        // Arrange
        var req = new CreatePositionRequest("AAPL", 10, 150m, DateTime.UtcNow);
        var listing = new StockListing { Id = 1, TickerSymbol = "AAPL", Name = "Apple" };
        _uow.Setup(u => u.StockListings.FindBySymbolAsync("AAPL", Ct)).ReturnsAsync(listing);
        _uow.Setup(u => u.WatchlistItems.GetByUserAndSymbolAsync(UserId, "AAPL", Ct)).ReturnsAsync((WatchlistItem?)null);
        _uow.Setup(u => u.Trades.GetByUserAndSymbolAsync(UserGuid, "AAPL", Ct)).ReturnsAsync(new List<Trade> { new() { TickerSymbol = "AAPL", Type = TradeType.Buy, Quantity = 10, UnitPrice = 150m } });
        _stockData.Setup(s => s.GetQuoteAsync("AAPL", Ct)).ReturnsAsync(new StockQuoteResponse("AAPL", 160m, 0m, 0, 0m, 0m, 0m, 0m, DateTime.UtcNow));

        // Act
        var result = await _sut.OpenPositionAsync(req, UserId, Ct);

        // Assert
        result.Should().NotBeNull();
        result.Symbol.Should().Be("AAPL");
    }

    [Fact]
    public async Task RecordTradeAsync_SuccessfullyRecordsBuyTrade()
    {
        // Arrange
        var req = new TradeRequest(TradeType.Buy, 5, 160m, "Buying more");
        var listing = new StockListing { Id = 1, TickerSymbol = "AAPL", Name = "Apple" };
        _uow.Setup(u => u.Trades.GetNetHoldingsAsync(UserGuid, "AAPL", Ct)).ReturnsAsync(10m);
        _uow.Setup(u => u.StockListings.FindBySymbolAsync("AAPL", Ct)).ReturnsAsync(listing);
        _uow.Setup(u => u.Trades.GetByUserAndSymbolAsync(UserGuid, "AAPL", Ct)).ReturnsAsync(new List<Trade> { new() { TickerSymbol = "AAPL", Type = TradeType.Buy, Quantity = 15, UnitPrice = 155m } });
        _stockData.Setup(s => s.GetQuoteAsync("AAPL", Ct)).ReturnsAsync(new StockQuoteResponse("AAPL", 160m, 0m, 0, 0m, 0m, 0m, 0m, DateTime.UtcNow));

        // Act
        var result = await _sut.RecordTradeAsync("AAPL", req, UserId, Ct);

        // Assert
        result.Should().NotBeNull();
        _uow.Verify(u => u.Trades.AddAsync(It.Is<Trade>(t => t.Notes == "Buying more"), Ct), Times.Once);
    }
}
