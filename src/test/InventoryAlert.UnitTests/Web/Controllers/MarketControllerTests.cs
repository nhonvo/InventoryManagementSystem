using InventoryAlert.Api.Controllers;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Web.Controllers;

public class MarketControllerTests
{
    private readonly Mock<IStockDataService> _stockDataServiceMock = new();

    [Fact]
    public async Task GetStatus_ReturnsOkWithData()
    {
        // Arrange
        var controller = new MarketController(_stockDataServiceMock.Object);
        var expected = new List<MarketStatusResponse>
        {
            new MarketStatusResponse("US", true, "Regular", null, "US/Eastern")
        };

        _stockDataServiceMock.Setup(s => s.GetMarketStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var actionResult = await controller.GetStatus(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var actual = Assert.IsType<List<MarketStatusResponse>>(okResult.Value);
        Assert.Single(actual);
    }

    [Fact]
    public async Task GetNews_ReturnsOkWithNewsList()
    {
        // Arrange
        var controller = new MarketController(_stockDataServiceMock.Object);
        var expected = new List<NewsResponse>
        {
            new NewsResponse(100, "Headline", "Summary", "Source", "http://url", DateTime.UtcNow, "http://image", "general")
        };

        _stockDataServiceMock.Setup(s => s.GetMarketNewsAsync("general", 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var actionResult = await controller.GetNews("general", 1, 10, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var actual = Assert.IsType<List<NewsResponse>>(okResult.Value);
        Assert.Single(actual);
    }

    [Fact]
    public async Task GetHolidays_ReturnsBadRequest_WhenExchangeIsEmpty()
    {
        // Arrange
        var controller = new MarketController(_stockDataServiceMock.Object);

        // Act
        var actionResult = await controller.GetHolidays("", CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task GetHolidays_ReturnsOk_WhenExchangeIsValid()
    {
        // Arrange
        var controller = new MarketController(_stockDataServiceMock.Object);
        var expected = new List<MarketHolidayResponse>
        {
            new MarketHolidayResponse("US", "New Year", DateOnly.FromDateTime(DateTime.UtcNow), "Closed")
        };

        _stockDataServiceMock.Setup(s => s.GetMarketHolidaysAsync("US", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var actionResult = await controller.GetHolidays("US", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var actual = Assert.IsType<List<MarketHolidayResponse>>(okResult.Value);
        Assert.Single(actual);
    }

    [Fact]
    public async Task GetEarningsCalendar_ReturnsOkData()
    {
        // Arrange
        var controller = new MarketController(_stockDataServiceMock.Object);
        var expected = new List<EarningsCalendarResponse>
        {
            new EarningsCalendarResponse("AAPL", DateOnly.FromDateTime(DateTime.UtcNow), 1.5m, 1.4m, 100m, 95m)
        };

        _stockDataServiceMock.Setup(s => s.GetEarningsCalendarAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), "AAPL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var actionResult = await controller.GetEarningsCalendar(null, null, "AAPL", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var actual = Assert.IsType<List<EarningsCalendarResponse>>(okResult.Value);
        Assert.Single(actual);
    }

    [Fact]
    public async Task GetIpoCalendar_ReturnsOkData()
    {
        // Arrange
        var controller = new MarketController(_stockDataServiceMock.Object);
        var expected = new List<IpoCalendarResponse>
        {
            new IpoCalendarResponse("NEW", "New Corp", DateOnly.FromDateTime(DateTime.UtcNow), 10.0m, 1000000, "Expected")
        };

        _stockDataServiceMock.Setup(s => s.GetIpoCalendarAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var actionResult = await controller.GetIpoCalendar(null, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var actual = Assert.IsType<List<IpoCalendarResponse>>(okResult.Value);
        Assert.Single(actual);
    }
}
