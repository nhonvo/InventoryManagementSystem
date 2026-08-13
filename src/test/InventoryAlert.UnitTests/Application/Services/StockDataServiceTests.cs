using System.Text.Json;
using FluentAssertions;
using InventoryAlert.Api.Services;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Dynamodb;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.External.Finnhub;
using InventoryAlert.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace InventoryAlert.UnitTests.Application.Services;

public class StockDataServiceTests
{
    private readonly Mock<IFinnhubClient> _finnhub = new();
    private readonly Mock<IConnectionMultiplexer> _redis = new();
    private readonly Mock<IDatabase> _cache = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMarketNewsDynamoRepository> _marketNewsRepo = new();
    private readonly Mock<ICompanyNewsDynamoRepository> _companyNewsRepo = new();
    private readonly Mock<ILogger<StockDataService>> _logger = new();
    private readonly StockDataService _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public StockDataServiceTests()
    {
        _uow.Setup(u => u.StockListings).Returns(new Mock<IStockListingRepository>().Object);
        _uow.Setup(u => u.Metrics).Returns(new Mock<IStockMetricRepository>().Object);
        _uow.Setup(u => u.Earnings).Returns(new Mock<IEarningsSurpriseRepository>().Object);
        _uow.Setup(u => u.Recommendations).Returns(new Mock<IRecommendationTrendRepository>().Object);
        _uow.Setup(u => u.Insiders).Returns(new Mock<IInsiderTransactionRepository>().Object);

        // Mock ExecuteSynchronizedAsync to execute the delegate
        _uow.Setup(u => u.ExecuteSynchronizedAsync(It.IsAny<Func<Task<StockListing?>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<StockListing?>>, CancellationToken>((func, ct) => func());

        _uow.Setup(u => u.ExecuteSynchronizedAsync(It.IsAny<Func<Task<StockListing>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<StockListing>>, CancellationToken>((func, ct) => func());

        _uow.Setup(u => u.ExecuteSynchronizedAsync(It.IsAny<Func<Task<StockMetric?>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<StockMetric?>>, CancellationToken>((func, ct) => func());

        _redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_cache.Object);

        _sut = new StockDataService(
            _finnhub.Object,
            _redis.Object,
            _uow.Object,
            _marketNewsRepo.Object,
            _companyNewsRepo.Object,
            _logger.Object);
    }

    [Fact]
    public async Task GetQuote_ReturnsCachedResult_WhenAvailable()
    {
        var symbol = "AAPL";
        var cachedResponse = new StockQuoteResponse(symbol, 150m, 1m, 0.5, 152m, 148m, 149m, 149m, DateTime.UtcNow);
        var json = JsonSerializer.Serialize(cachedResponse, InventoryAlert.Domain.Configuration.JsonOptions.Default);

        _cache.Setup(c => c.StringGetAsync($"quote:{symbol}", It.IsAny<CommandFlags>()))
            .ReturnsAsync(json);

        var result = await _sut.GetQuoteAsync(symbol, Ct);

        result.Should().NotBeNull();
        result!.Symbol.Should().Be(symbol);
        result.Price.Should().Be(150m);
        _finnhub.Verify(f => f.GetQuoteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetQuote_DiscoveryFlow_PersistsNewListing_WhenMissing()
    {
        // Arrange
        var symbol = "TSLA";
        _cache.Setup(c => c.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(RedisValue.Null);
        _uow.Setup(u => u.StockListings.FindBySymbolAsync(symbol, Ct)).ReturnsAsync((StockListing?)null);

        var profile = new FinnhubProfileResponse { Name = "Tesla Inc", Exchange = "NASDAQ" };
        _finnhub.Setup(f => f.GetProfileAsync(symbol, Ct)).ReturnsAsync(profile);
        _finnhub.Setup(f => f.GetQuoteAsync(symbol, Ct)).ReturnsAsync(new FinnhubQuoteResponse { CurrentPrice = 200m });

        // Act
        await _sut.GetQuoteAsync(symbol, Ct);

        // Assert
        _uow.Verify(u => u.StockListings.AddAsync(It.Is<StockListing>(l => l.TickerSymbol == symbol && l.Name == "Tesla Inc"), Ct), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }

    [Fact]
    public async Task GetFinancials_ReturnsResults_FromDatabase()
    {
        // Arrange
        var symbol = "MSFT";
        var metric = new StockMetric { TickerSymbol = symbol, PeRatio = 35.5 };
        _cache.Setup(c => c.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(RedisValue.Null);
        _uow.Setup(u => u.Metrics.GetBySymbolAsync(symbol, Ct)).ReturnsAsync(metric);

        // Act
        var result = await _sut.GetFinancialsAsync(symbol, Ct);

        // Assert
        result.Should().NotBeNull();
        result!.PeRatio.Should().Be(35.5);
    }

    [Fact]
    public async Task GetPeers_CachesResult_ForOneDay()
    {
        // Arrange
        var symbol = "AMD";
        var peers = new List<string> { "INTC", "NVDA" };
        _cache.Setup(c => c.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(RedisValue.Null);
        _finnhub.Setup(f => f.GetPeersAsync(symbol, Ct)).ReturnsAsync(peers);

        // Act
        var result = await _sut.GetPeersAsync(symbol, Ct);

        // Assert
        result.Should().NotBeNull();
        result!.Peers.Should().Contain("NVDA");
    }

    [Fact]
    public async Task GetMarketStatus_FetchesMajorExchanges()
    {
        // Arrange
        _finnhub.Setup(f => f.GetMarketStatusAsync(It.IsAny<string>(), Ct))
            .ReturnsAsync(new FinnhubMarketStatus { IsOpen = true, Exchange = "US" });

        // Act
        var result = await _sut.GetMarketStatusAsync(Ct);

        // Assert
        result.Should().NotBeEmpty();
        _finnhub.Verify(f => f.GetMarketStatusAsync(It.IsAny<string>(), Ct), Times.AtLeast(3));
    }

    [Fact]
    public async Task GetCatalogAsync_ReturnsPagedListings()
    {
        // Arrange
        var listings = new List<StockListing>
        {
            new() { TickerSymbol = "AAPL", Name = "Apple", Exchange = "NASDAQ", Industry = "Tech" },
            new() { TickerSymbol = "MSFT", Name = "Microsoft", Exchange = "NASDAQ", Industry = "Tech" }
        };
        _uow.Setup(u => u.StockListings.GetAllAsync(Ct)).ReturnsAsync(listings);

        // Act
        var result = await _sut.GetCatalogAsync(1, 10, "NASDAQ", "Tech", Ct);

        // Assert
        result.TotalItems.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsProfileResponse_WhenListingExists()
    {
        // Arrange
        var listing = new StockListing { TickerSymbol = "AAPL", Name = "Apple", Exchange = "NASDAQ" };
        _uow.Setup(u => u.StockListings.FindBySymbolAsync("AAPL", Ct)).ReturnsAsync(listing);

        // Act
        var result = await _sut.GetProfileAsync("AAPL", Ct);

        // Assert
        result.Should().NotBeNull();
        result!.Symbol.Should().Be("AAPL");
    }

    [Fact]
    public async Task GetEarningsAsync_ReturnsMappedEarningsResponses()
    {
        // Arrange
        var earnings = new List<EarningsSurprise>
        {
            new() { TickerSymbol = "AAPL", Period = new DateOnly(2026, 1, 1), ActualEps = 1.5, EstimateEps = 1.4 }
        };
        _uow.Setup(u => u.ExecuteSynchronizedAsync(It.IsAny<Func<Task<IEnumerable<EarningsSurprise>>>>(), Ct)).ReturnsAsync(earnings);

        // Act
        var result = await _sut.GetEarningsAsync("AAPL", Ct);

        // Assert
        result.Should().HaveCount(1);
        result.First().ActualEps.Should().Be(1.5);
    }

    [Fact]
    public async Task GetRecommendationsAsync_ReturnsMappedRecommendationResponses()
    {
        // Arrange
        var recs = new List<RecommendationTrend>
        {
            new() { TickerSymbol = "AAPL", Period = "2026-01-01", Buy = 20, Hold = 5 }
        };
        _uow.Setup(u => u.ExecuteSynchronizedAsync(It.IsAny<Func<Task<IEnumerable<RecommendationTrend>>>>(), Ct)).ReturnsAsync(recs);

        // Act
        var result = await _sut.GetRecommendationsAsync("AAPL", Ct);

        // Assert
        result.Should().HaveCount(1);
        result.First().Buy.Should().Be(20);
    }

    [Fact]
    public async Task GetInsidersAsync_ReturnsMappedInsiderResponses()
    {
        // Arrange
        var insiders = new List<InsiderTransaction>
        {
            new() { TickerSymbol = "AAPL", Name = "Tim Cook", Share = 1000 }
        };
        _uow.Setup(u => u.ExecuteSynchronizedAsync(It.IsAny<Func<Task<IEnumerable<InsiderTransaction>>>>(), Ct)).ReturnsAsync(insiders);

        // Act
        var result = await _sut.GetInsidersAsync("AAPL", Ct);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Tim Cook");
    }

    [Fact]
    public async Task SearchSymbolsAsync_ReturnsMappedSearchResults()
    {
        // Arrange
        _cache.Setup(c => c.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(RedisValue.Null);
        var searchResult = new FinnhubSymbolSearch
        {
            Result = [new FinnhubSymbolResult { Symbol = "AAPL", Description = "Apple Inc", Type = "Common Stock" }]
        };
        _finnhub.Setup(f => f.SearchSymbolsAsync("AAPL", Ct)).ReturnsAsync(searchResult);

        // Act
        var result = await _sut.SearchSymbolsAsync("AAPL", Ct);

        // Assert
        result.Should().HaveCount(1);
        result.First().Symbol.Should().Be("AAPL");
    }

    [Fact]
    public async Task GetMarketHolidaysAsync_ReturnsMappedHolidays()
    {
        // Arrange
        var holidays = new List<FinnhubHoliday>
        {
            new() { EventName = "New Year", AtDate = "2026-01-01", TradingHour = "Closed" }
        };
        _finnhub.Setup(f => f.GetMarketHolidaysAsync("US", Ct)).ReturnsAsync(holidays);

        // Act
        var result = await _sut.GetMarketHolidaysAsync("US", Ct);

        // Assert
        result.Should().HaveCount(1);
        result.First().EventName.Should().Be("New Year");
    }

    [Fact]
    public async Task GetEarningsCalendarAsync_ReturnsMappedCalendar()
    {
        // Arrange
        var calendar = new FinnhubEarningsCalendar
        {
            Earnings = [new FinnhubEarningsItem { Symbol = "AAPL", Date = "2026-01-01", EpsEstimate = 1.5m }]
        };
        _finnhub.Setup(f => f.GetEarningsCalendarAsync(It.IsAny<string>(), It.IsAny<string>(), Ct)).ReturnsAsync(calendar);

        // Act
        var result = await _sut.GetEarningsCalendarAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), null, Ct);

        // Assert
        result.Should().HaveCount(1);
        result.First().Symbol.Should().Be("AAPL");
    }

    [Fact]
    public async Task GetIpoCalendarAsync_ReturnsMappedIpos()
    {
        // Arrange
        var ipoCalendar = new FinnhubIpoCalendar
        {
            Items = [new FinnhubIpoItem { Symbol = "NEW", Name = "New Corp", Date = "2026-02-01", Price = "20.0" }]
        };
        _finnhub.Setup(f => f.GetIpoCalendarAsync(It.IsAny<string>(), It.IsAny<string>(), Ct)).ReturnsAsync(ipoCalendar);

        // Act
        var result = await _sut.GetIpoCalendarAsync(new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28), Ct);

        // Assert
        result.Should().HaveCount(1);
        result.First().Symbol.Should().Be("NEW");
        result.First().Price.Should().Be(20.0m);
    }

    [Fact]
    public async Task GetCompanyNewsAsync_ReturnsFromDynamo_WhenPresent()
    {
        // Arrange
        var entries = new List<InventoryAlert.Domain.Entities.Dynamodb.CompanyNewsDynamoEntry>
        {
            new() { NewsId = 1, Symbol = "AAPL", Headline = "Apple Earnings", Summary = "Good news", Source = "Bloomberg", Url = "https://example.com", ImageUrl = "", Timestamp = 1700000000 }
        };
        _companyNewsRepo.Setup(c => c.GetLatestBySymbolAsync("AAPL", 10, Ct)).ReturnsAsync(entries);

        // Act
        var result = await _sut.GetCompanyNewsAsync("AAPL", 1, 10, Ct);

        // Assert
        result.Should().HaveCount(1);
        result.First().Headline.Should().Be("Apple Earnings");
    }

    [Fact]
    public async Task GetMarketNewsAsync_ReturnsFromDynamo_WhenPresent()
    {
        // Arrange
        var entries = new List<InventoryAlert.Domain.Entities.Dynamodb.MarketNewsDynamoEntry>
        {
            new() { NewsId = 2, Category = "general", Headline = "Market Rally", Summary = "Stocks up", Source = "Reuters", Url = "https://example.com", ImageUrl = "", PublishedAt = DateTime.UtcNow.ToString("o") }
        };
        _marketNewsRepo.Setup(m => m.QueryAsync("CATEGORY#GENERAL", Ct)).ReturnsAsync(entries);

        // Act
        var result = await _sut.GetMarketNewsAsync("general", 1, 20, Ct);

        // Assert
        result.Should().HaveCount(1);
        result.First().Headline.Should().Be("Market Rally");
    }

    [Fact]
    public async Task GetFinancials_ReturnsCachedResult_WhenAvailable()
    {
        // Arrange
        var symbol = "AAPL";
        var cachedResponse = new StockMetricResponse(symbol, 30.0, 10.0, 6.5, 0.6, 200.0m, 150.0m, 0.15, 0.25, DateTime.UtcNow);
        var json = JsonSerializer.Serialize(cachedResponse, InventoryAlert.Domain.Configuration.JsonOptions.Default);
        _cache.Setup(c => c.StringGetAsync($"metrics:{symbol}", It.IsAny<CommandFlags>())).ReturnsAsync(json);

        // Act
        var result = await _sut.GetFinancialsAsync(symbol, Ct);

        // Assert
        result.Should().NotBeNull();
        result!.Symbol.Should().Be(symbol);
    }

    [Fact]
    public async Task GetCompanyNewsAsync_FallbackToFinnhub_WhenDynamoEmpty()
    {
        // Arrange
        var symbol = "AAPL";
        _companyNewsRepo.Setup(c => c.GetLatestBySymbolAsync(symbol, 10, Ct)).ReturnsAsync(new List<CompanyNewsDynamoEntry>());
        var finnhubNews = new List<FinnhubNewsItem>
        {
            new() { Id = 10, Headline = "Finnhub News", Datetime = 1700000000, Source = "Bloomberg" }
        };
        _finnhub.Setup(f => f.GetCompanyNewsAsync(symbol, It.IsAny<string>(), It.IsAny<string>(), Ct)).ReturnsAsync(finnhubNews);

        // Act
        var result = await _sut.GetCompanyNewsAsync(symbol, 1, 10, Ct);

        // Assert
        result.Should().HaveCount(1);
        result.First().Headline.Should().Be("Finnhub News");
        _companyNewsRepo.Verify(c => c.BatchSaveAsync(It.IsAny<IEnumerable<CompanyNewsDynamoEntry>>(), Ct), Times.Once);
    }

    [Fact]
    public async Task GetMarketNewsAsync_FallbackToFinnhub_WhenDynamoEmpty()
    {
        // Arrange
        var category = "crypto";
        _marketNewsRepo.Setup(m => m.QueryAsync("CATEGORY#CRYPTO", Ct)).ReturnsAsync(new List<MarketNewsDynamoEntry>());
        var finnhubNews = new List<FinnhubNewsItem>
        {
            new() { Id = 20, Headline = "Crypto News", Datetime = 1700000000, Category = "crypto" }
        };
        _finnhub.Setup(f => f.GetMarketNewsAsync(category, Ct)).ReturnsAsync(finnhubNews);

        // Act
        var result = await _sut.GetMarketNewsAsync(category, 1, 20, Ct);

        // Assert
        result.Should().HaveCount(1);
        result.First().Headline.Should().Be("Crypto News");
        _marketNewsRepo.Verify(m => m.BatchSaveAsync(It.IsAny<IEnumerable<MarketNewsDynamoEntry>>(), Ct), Times.Once);
    }

    [Fact]
    public async Task GetPeers_ReturnsCachedResult_WhenAvailable()
    {
        // Arrange
        var symbol = "AMD";
        var cachedPeers = new PeersResponse(symbol, new List<string> { "INTC", "NVDA" });
        var json = JsonSerializer.Serialize(cachedPeers, InventoryAlert.Domain.Configuration.JsonOptions.Default);
        _cache.Setup(c => c.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(json);

        // Act
        var result = await _sut.GetPeersAsync(symbol, Ct);

        // Assert
        result.Should().NotBeNull();
        result!.Peers.Should().Contain("INTC");
        _finnhub.Verify(f => f.GetPeersAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchSymbolsAsync_ReturnsCachedResult_WhenAvailable()
    {
        // Arrange
        var query = "AAPL";
        var cachedSearch = new List<SymbolSearchResponse> { new("AAPL", "Apple Inc", "Common Stock", "") };
        var json = JsonSerializer.Serialize(cachedSearch, InventoryAlert.Domain.Configuration.JsonOptions.Default);
        _cache.Setup(c => c.StringGetAsync($"search:{query}", It.IsAny<CommandFlags>())).ReturnsAsync(json);

        // Act
        var result = await _sut.SearchSymbolsAsync(query, Ct);

        // Assert
        result.Should().HaveCount(1);
        result.First().Symbol.Should().Be("AAPL");
        _finnhub.Verify(f => f.SearchSymbolsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetQuote_ReturnsZeroQuote_WhenFinnhubReturnsNullAndListingExists()
    {
        // Arrange
        var symbol = "UNKNOWN";
        _cache.Setup(c => c.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(RedisValue.Null);
        var listing = new StockListing { TickerSymbol = symbol, Name = "Unknown Corp" };
        _uow.Setup(u => u.StockListings.FindBySymbolAsync(symbol, Ct)).ReturnsAsync(listing);
        _finnhub.Setup(f => f.GetQuoteAsync(symbol, Ct)).ReturnsAsync((FinnhubQuoteResponse?)null);

        // Act
        var result = await _sut.GetQuoteAsync(symbol, Ct);

        // Assert
        result.Should().NotBeNull();
        result!.Price.Should().Be(0m);
    }

    [Fact]
    public async Task GetEarningsCalendarAsync_FiltersBySymbol_WhenSymbolProvided()
    {
        // Arrange
        var calendar = new FinnhubEarningsCalendar
        {
            Earnings = [
                new FinnhubEarningsItem { Symbol = "AAPL", Date = "2026-01-01", EpsEstimate = 1.5m },
                new FinnhubEarningsItem { Symbol = "MSFT", Date = "2026-01-01", EpsEstimate = 2.5m }
            ]
        };
        _finnhub.Setup(f => f.GetEarningsCalendarAsync(It.IsAny<string>(), It.IsAny<string>(), Ct)).ReturnsAsync(calendar);

        // Act
        var result = await _sut.GetEarningsCalendarAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), "AAPL", Ct);

        // Assert
        result.Should().HaveCount(1);
        result.First().Symbol.Should().Be("AAPL");
    }
}
