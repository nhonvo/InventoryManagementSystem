using FluentAssertions;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.External.Finnhub;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Worker.Models;
using InventoryAlert.Worker.ScheduledJobs;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Worker.ScheduledJobs;

public class SyncStockFundamentalsJobTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IFinnhubClient> _finnhubMock = new();
    private readonly Mock<ILogger<SyncStockFundamentalsJob>> _loggerMock = new();
    private readonly SyncStockFundamentalsJob _sut;

    public SyncStockFundamentalsJobTests()
    {
        _uowMock.Setup(u => u.StockListings).Returns(new Mock<IStockListingRepository>().Object);
        _uowMock.Setup(u => u.Metrics).Returns(new Mock<IStockMetricRepository>().Object);
        _uowMock.Setup(u => u.Earnings).Returns(new Mock<IEarningsSurpriseRepository>().Object);
        _uowMock.Setup(u => u.Recommendations).Returns(new Mock<IRecommendationTrendRepository>().Object);
        _uowMock.Setup(u => u.Insiders).Returns(new Mock<IInsiderTransactionRepository>().Object);

        _sut = new SyncStockFundamentalsJob(_uowMock.Object, _finnhubMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_SyncsFundamentals_ForActiveSymbols()
    {
        // Arrange
        _uowMock.Setup(u => u.StockListings.GetActiveSymbolsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "AAPL" });

        var metricResp = new FinnhubMetricsResponse
        {
            Metric = new Dictionary<string, double> { { "peExclExtraTTM", 30.0 }, { "52WeekHigh", 200.0 }, { "52WeekLow", 130.0 } }
        };
        _finnhubMock.Setup(f => f.GetMetricsAsync("AAPL", It.IsAny<CancellationToken>())).ReturnsAsync(metricResp);

        var earnings = new List<FinnhubEarnings> { new() { Period = "2024-03-31", Actual = 1.5, Estimate = 1.4 } };
        _finnhubMock.Setup(f => f.GetEarningsAsync("AAPL", It.IsAny<CancellationToken>())).ReturnsAsync(earnings);

        var recs = new List<FinnhubRecommendation> { new() { Period = "2024-04", Buy = 20, Hold = 5 } };
        _finnhubMock.Setup(f => f.GetRecommendationsAsync("AAPL", It.IsAny<CancellationToken>())).ReturnsAsync(recs);

        var insiders = new FinnhubInsiderResponse
        {
            Data = new List<FinnhubInsiderItem> { new() { Name = "Cook Tim", Share = 1000, TransactionPrice = 175m, FilingDate = "2024-04-01" } }
        };
        _finnhubMock.Setup(f => f.GetInsidersAsync("AAPL", It.IsAny<CancellationToken>())).ReturnsAsync(insiders);

        // Act
        var result = await _sut.ExecuteAsync(CancellationToken.None);

        // Assert
        result.Status.Should().Be(JobStatus.Success);
        _uowMock.Verify(u => u.Metrics.UpsertAsync(It.Is<StockMetric>(m => m.TickerSymbol == "AAPL"), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.Earnings.UpsertRangeAsync(It.IsAny<IEnumerable<EarningsSurprise>>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.Recommendations.UpsertRangeAsync(It.IsAny<IEnumerable<RecommendationTrend>>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.Insiders.ReplaceForSymbolAsync("AAPL", It.IsAny<IEnumerable<InsiderTransaction>>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
