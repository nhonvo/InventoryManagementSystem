using FluentAssertions;
using InventoryAlert.Api.Services;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Application.Services;

public class WatchlistServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IWatchlistItemRepository> _watchlistRepoMock = new();
    private readonly Mock<IStockListingRepository> _stockListingRepoMock = new();
    private readonly Mock<IStockDataService> _stockDataServiceMock = new();
    private readonly Mock<ILogger<WatchlistService>> _loggerMock = new();
    private readonly WatchlistService _sut;
    private static readonly Guid TestUserGuid = Guid.NewGuid();
    private static readonly string TestUserId = TestUserGuid.ToString();
    private static readonly CancellationToken Ct = CancellationToken.None;

    public WatchlistServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.WatchlistItems).Returns(_watchlistRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.StockListings).Returns(_stockListingRepoMock.Object);
        _unitOfWorkMock
            .Setup(u => u.ExecuteTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((action, _) => action());

        _sut = new WatchlistService(_unitOfWorkMock.Object, _stockDataServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetWatchlistAsync_ReturnsPositionResponses_ForUserWatchlistItems()
    {
        // Arrange
        var items = new List<WatchlistItem>
        {
            new() { UserId = TestUserGuid, TickerSymbol = "AAPL", CreatedAt = DateTime.UtcNow }
        };
        _watchlistRepoMock.Setup(w => w.GetByUserIdAsync(TestUserId, Ct)).ReturnsAsync(items);

        var listing = new StockListing { Id = 1, TickerSymbol = "AAPL", Name = "Apple Inc.", Exchange = "NASDAQ" };
        _stockListingRepoMock.Setup(s => s.FindBySymbolAsync("AAPL", Ct)).ReturnsAsync(listing);
        _stockDataServiceMock.Setup(s => s.GetQuoteAsync("AAPL", Ct))
            .ReturnsAsync(new StockQuoteResponse("AAPL", 150m, 2m, 1.35, 155m, 149m, 150m, 148m, DateTime.UtcNow));

        // Act
        var result = await _sut.GetWatchlistAsync(TestUserId, Ct);

        // Assert
        result.Should().HaveCount(1);
        result.First().Symbol.Should().Be("AAPL");
        result.First().CurrentPrice.Should().Be(150m);
    }

    [Fact]
    public async Task AddToWatchlistAsync_ReturnsNull_WhenAlreadyOnWatchlist()
    {
        // Arrange
        _watchlistRepoMock.Setup(w => w.GetByUserAndSymbolAsync(TestUserId, "AAPL", Ct))
            .ReturnsAsync(new WatchlistItem { UserId = TestUserGuid, TickerSymbol = "AAPL" });

        // Act
        var result = await _sut.AddToWatchlistAsync("AAPL", TestUserId, Ct);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddToWatchlistAsync_AddsNewItem_WhenListingExists()
    {
        // Arrange
        _watchlistRepoMock.Setup(w => w.GetByUserAndSymbolAsync(TestUserId, "AAPL", Ct))
            .ReturnsAsync((WatchlistItem?)null);

        var listing = new StockListing { Id = 1, TickerSymbol = "AAPL", Name = "Apple Inc.", Exchange = "NASDAQ" };
        _stockListingRepoMock.Setup(s => s.FindBySymbolAsync("AAPL", Ct)).ReturnsAsync(listing);
        _stockDataServiceMock.Setup(s => s.GetQuoteAsync("AAPL", Ct))
            .ReturnsAsync(new StockQuoteResponse("AAPL", 150m, 2m, 1.35, 155m, 149m, 150m, 148m, DateTime.UtcNow));

        // Act
        var result = await _sut.AddToWatchlistAsync("AAPL", TestUserId, Ct);

        // Assert
        result.Should().NotBeNull();
        result!.Symbol.Should().Be("AAPL");
        _watchlistRepoMock.Verify(w => w.AddAsync(It.IsAny<WatchlistItem>(), Ct), Times.Once);
    }

    [Fact]
    public async Task RemoveFromWatchlistAsync_ThrowsKeyNotFound_WhenNotOnWatchlist()
    {
        // Arrange
        _watchlistRepoMock.Setup(w => w.GetByUserAndSymbolAsync(TestUserId, "AAPL", Ct))
            .ReturnsAsync((WatchlistItem?)null);

        // Act
        Func<Task> act = () => _sut.RemoveFromWatchlistAsync("AAPL", TestUserId, Ct);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task RemoveFromWatchlistAsync_DeletesItem_WhenOnWatchlist()
    {
        // Arrange
        var item = new WatchlistItem { UserId = TestUserGuid, TickerSymbol = "AAPL" };
        _watchlistRepoMock.Setup(w => w.GetByUserAndSymbolAsync(TestUserId, "AAPL", Ct))
            .ReturnsAsync(item);

        // Act
        await _sut.RemoveFromWatchlistAsync("AAPL", TestUserId, Ct);

        // Assert
        _watchlistRepoMock.Verify(w => w.DeleteAsync(item, Ct), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }

    [Fact]
    public async Task GetWatchlistItemAsync_ReturnsPosition_WhenItemExists()
    {
        // Arrange
        _watchlistRepoMock.Setup(w => w.GetByUserAndSymbolAsync(TestUserId, "AAPL", Ct))
            .ReturnsAsync(new WatchlistItem { UserId = TestUserGuid, TickerSymbol = "AAPL" });
        var listing = new StockListing { Id = 1, TickerSymbol = "AAPL", Name = "Apple Inc.", Exchange = "NASDAQ" };
        _stockListingRepoMock.Setup(s => s.FindBySymbolAsync("AAPL", Ct)).ReturnsAsync(listing);
        _stockDataServiceMock.Setup(s => s.GetQuoteAsync("AAPL", Ct))
            .ReturnsAsync(new StockQuoteResponse("AAPL", 150m, 2m, 1.35, 155m, 149m, 150m, 148m, DateTime.UtcNow));

        // Act
        var result = await _sut.GetWatchlistItemAsync("AAPL", TestUserId, Ct);

        // Assert
        result.Should().NotBeNull();
        result!.Symbol.Should().Be("AAPL");
    }

    [Fact]
    public async Task GetWatchlistItemAsync_ReturnsNull_WhenItemDoesNotExist()
    {
        // Arrange
        _watchlistRepoMock.Setup(w => w.GetByUserAndSymbolAsync(TestUserId, "MSFT", Ct))
            .ReturnsAsync((WatchlistItem?)null);

        // Act
        var result = await _sut.GetWatchlistItemAsync("MSFT", TestUserId, Ct);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddToWatchlistAsync_ThrowsKeyNotFound_WhenSymbolUnresolvable()
    {
        // Arrange
        _watchlistRepoMock.Setup(w => w.GetByUserAndSymbolAsync(TestUserId, "UNKNOWN", Ct))
            .ReturnsAsync((WatchlistItem?)null);
        _stockListingRepoMock.Setup(s => s.FindBySymbolAsync("UNKNOWN", Ct)).ReturnsAsync((StockListing?)null);
        _stockDataServiceMock.Setup(s => s.GetProfileAsync("UNKNOWN", Ct)).ReturnsAsync((StockProfileResponse?)null);

        // Act
        Func<Task> act = () => _sut.AddToWatchlistAsync("UNKNOWN", TestUserId, Ct);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetWatchlistAsync_SkipsItems_WhenBuildPositionResponseReturnsNull()
    {
        // Arrange
        var items = new List<WatchlistItem>
        {
            new() { UserId = TestUserGuid, TickerSymbol = "UNKNOWN", CreatedAt = DateTime.UtcNow }
        };
        _watchlistRepoMock.Setup(w => w.GetByUserIdAsync(TestUserId, Ct)).ReturnsAsync(items);
        _stockListingRepoMock.Setup(s => s.FindBySymbolAsync("UNKNOWN", Ct)).ReturnsAsync((StockListing?)null);
        _stockDataServiceMock.Setup(s => s.GetProfileAsync("UNKNOWN", Ct)).ReturnsAsync((StockProfileResponse?)null);

        // Act
        var result = await _sut.GetWatchlistAsync(TestUserId, Ct);

        // Assert
        result.Should().BeEmpty();
    }
}
