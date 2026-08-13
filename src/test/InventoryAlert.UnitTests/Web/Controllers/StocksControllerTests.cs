using InventoryAlert.Api.Controllers;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Web.Controllers;

public class StocksControllerTests
{
    private readonly Mock<IStockDataService> _stockDataServiceMock = new();

    [Fact]
    public async Task GetCatalog_ReturnsOkPagedResult()
    {
        // Arrange
        var controller = new StocksController(_stockDataServiceMock.Object);
        var paged = new PagedResult<StockProfileResponse>
        {
            Items = new List<StockProfileResponse> { new("AAPL", "Apple Inc.", "NASDAQ", "USD", "US", "Tech", 3000000000m, null, "http://apple.com", "http://logo") },
            TotalItems = 1,
            PageNumber = 1,
            PageSize = 20
        };

        _stockDataServiceMock.Setup(s => s.GetCatalogAsync(1, 20, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        // Act
        var actionResult = await controller.GetCatalog(1, 20, null, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var actual = Assert.IsType<PagedResult<StockProfileResponse>>(okResult.Value);
        Assert.Equal(1, actual.TotalItems);
    }

    [Fact]
    public async Task Search_ReturnsOkSymbolResults()
    {
        // Arrange
        var controller = new StocksController(_stockDataServiceMock.Object);
        var expected = new List<SymbolSearchResponse>
        {
            new("AAPL", "Apple Inc.", "Common Stock", "US")
        };

        _stockDataServiceMock.Setup(s => s.SearchSymbolsAsync("AAPL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var actionResult = await controller.Search("AAPL", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var actual = Assert.IsType<List<SymbolSearchResponse>>(okResult.Value);
        Assert.Single(actual);
    }

    [Fact]
    public async Task GetQuote_ReturnsOk_WhenSymbolFound()
    {
        // Arrange
        var controller = new StocksController(_stockDataServiceMock.Object);
        var quote = new StockQuoteResponse("AAPL", 150m, 2m, 1.35, 152m, 148m, 149m, 148m, DateTime.UtcNow);

        _stockDataServiceMock.Setup(s => s.GetQuoteAsync("AAPL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        // Act
        var actionResult = await controller.GetQuote("AAPL", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var actual = Assert.IsType<StockQuoteResponse>(okResult.Value);
        Assert.Equal("AAPL", actual.Symbol);
    }

    [Fact]
    public async Task GetQuote_ReturnsNotFound_WhenSymbolNotFound()
    {
        // Arrange
        var controller = new StocksController(_stockDataServiceMock.Object);
        _stockDataServiceMock.Setup(s => s.GetQuoteAsync("UNKNOWN", It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockQuoteResponse?)null);

        // Act
        var actionResult = await controller.GetQuote("UNKNOWN", CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(actionResult.Result);
    }

    [Fact]
    public async Task GetProfile_ReturnsOk_WhenFound()
    {
        // Arrange
        var controller = new StocksController(_stockDataServiceMock.Object);
        var profile = new StockProfileResponse("AAPL", "Apple Inc.", "NASDAQ", "USD", "US", "Tech", 3000000000m, null, "http://apple.com", "http://logo");

        _stockDataServiceMock.Setup(s => s.GetProfileAsync("AAPL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var actionResult = await controller.GetProfile("AAPL", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetPeers_ReturnsOk_WhenFound()
    {
        // Arrange
        var controller = new StocksController(_stockDataServiceMock.Object);
        var peers = new PeersResponse("AAPL", new List<string> { "MSFT", "GOOGL" });

        _stockDataServiceMock.Setup(s => s.GetPeersAsync("AAPL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(peers);

        // Act
        var actionResult = await controller.GetPeers("AAPL", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var actual = Assert.IsType<PeersResponse>(okResult.Value);
        Assert.Equal(2, actual.Peers.Count);
    }
}
