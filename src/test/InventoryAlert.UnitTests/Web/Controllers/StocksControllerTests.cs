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

    [Fact]
    public async Task GetFinancials_ReturnsOk_WhenFound()
    {
        // Arrange
        var controller = new StocksController(_stockDataServiceMock.Object);
        var res = new StockMetricResponse("AAPL", 30.5, 10.2, 5.5, 0.6, 200, 140, 0.15, 0.25, DateTime.UtcNow);
        _stockDataServiceMock.Setup(s => s.GetFinancialsAsync("AAPL", It.IsAny<CancellationToken>())).ReturnsAsync(res);

        // Act
        var result = await controller.GetFinancials("AAPL", CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetFinancials_ReturnsNotFound_WhenNull()
    {
        // Arrange
        var controller = new StocksController(_stockDataServiceMock.Object);
        _stockDataServiceMock.Setup(s => s.GetFinancialsAsync("UNKNOWN", It.IsAny<CancellationToken>())).ReturnsAsync((StockMetricResponse?)null);

        // Act
        var result = await controller.GetFinancials("UNKNOWN", CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetEarnings_ReturnsOk()
    {
        // Arrange
        var controller = new StocksController(_stockDataServiceMock.Object);
        var list = new List<EarningsSurpriseResponse> { new(new DateOnly(2026, 1, 1), 1.5, 1.4, 7.1, new DateOnly(2026, 1, 1)) };
        _stockDataServiceMock.Setup(s => s.GetEarningsAsync("AAPL", It.IsAny<CancellationToken>())).ReturnsAsync(list);

        // Act
        var result = await controller.GetEarnings("AAPL", CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetRecommendations_ReturnsOk()
    {
        // Arrange
        var controller = new StocksController(_stockDataServiceMock.Object);
        var list = new List<RecommendationResponse> { new("2026-01-01", 10, 5, 2, 1, 0) };
        _stockDataServiceMock.Setup(s => s.GetRecommendationsAsync("AAPL", It.IsAny<CancellationToken>())).ReturnsAsync(list);

        // Act
        var result = await controller.GetRecommendations("AAPL", CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetInsiders_ReturnsOk()
    {
        // Arrange
        var controller = new StocksController(_stockDataServiceMock.Object);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var list = new List<InsiderTransactionResponse> { new("Tim Cook", 1000, 150000, today, today, "S") };
        _stockDataServiceMock.Setup(s => s.GetInsidersAsync("AAPL", It.IsAny<CancellationToken>())).ReturnsAsync(list);

        // Act
        var result = await controller.GetInsiders("AAPL", CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetNews_ReturnsOk()
    {
        // Arrange
        var controller = new StocksController(_stockDataServiceMock.Object);
        var list = new List<NewsResponse> { new(1, "Headline", "Summary", "Source", "Url", DateTime.UtcNow, "Image", "company") };
        _stockDataServiceMock.Setup(s => s.GetCompanyNewsAsync("AAPL", 1, 10, It.IsAny<CancellationToken>())).ReturnsAsync(list);

        // Act
        var result = await controller.GetNews("AAPL", 1, 10, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
    }
}
