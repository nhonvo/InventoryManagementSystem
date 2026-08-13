using System.Security.Claims;
using FluentAssertions;
using InventoryAlert.Api.Controllers;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Web.Controllers;

public class PortfolioControllerTests
{
    private readonly Mock<IPortfolioService> _serviceMock = new();
    private readonly PortfolioController _sut;
    private static readonly string TestUserId = Guid.NewGuid().ToString();
    private static readonly CancellationToken Ct = CancellationToken.None;

    public PortfolioControllerTests()
    {
        _sut = new PortfolioController(_serviceMock.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, TestUserId)
        ], "TestAuth"));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task GetPositions_ReturnsOkWithPagedResult()
    {
        // Arrange
        var query = new PortfolioQueryParams { PageNumber = 1, PageSize = 10 };
        var paged = new PagedResult<PortfolioPositionResponse> { Items = [], TotalItems = 0, PageNumber = 1, PageSize = 10 };
        _serviceMock.Setup(s => s.GetPositionsPagedAsync(query, TestUserId, Ct)).ReturnsAsync(paged);

        // Act
        var result = await _sut.GetPositions(query, Ct);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(paged);
    }

    [Fact]
    public async Task GetPosition_ReturnsOk_WhenExists()
    {
        // Arrange
        var pos = new PortfolioPositionResponse(1, "AAPL", "Apple", "NASDAQ", null, 10, 150m, 160m, 1600m, 1500m, 100m, 6.67, 2m, 1.25m, "Tech");
        _serviceMock.Setup(s => s.GetPositionBySymbolAsync("AAPL", TestUserId, Ct)).ReturnsAsync(pos);

        // Act
        var result = await _sut.GetPosition("AAPL", Ct);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(pos);
    }

    [Fact]
    public async Task GetPosition_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetPositionBySymbolAsync("UNKNOWN", TestUserId, Ct)).ReturnsAsync((PortfolioPositionResponse?)null);

        // Act
        var result = await _sut.GetPosition("UNKNOWN", Ct);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OpenPosition_ReturnsCreatedAtAction()
    {
        // Arrange
        var req = new CreatePositionRequest("AAPL", 10, 150m, null);
        var pos = new PortfolioPositionResponse(1, "AAPL", "Apple", "NASDAQ", null, 10, 150m, 160m, 1600m, 1500m, 100m, 6.67, 2m, 1.25m, "Tech");
        _serviceMock.Setup(s => s.OpenPositionAsync(req, TestUserId, Ct)).ReturnsAsync(pos);

        // Act
        var result = await _sut.OpenPosition(req, Ct);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.Value.Should().Be(pos);
    }

    [Fact]
    public async Task BulkImport_ReturnsOk()
    {
        // Arrange
        var requests = new List<CreatePositionRequest> { new("AAPL", 10, 150m, null) };

        // Act
        var result = await _sut.BulkImport(requests, Ct);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _serviceMock.Verify(s => s.BulkImportPositionsAsync(requests, TestUserId, Ct), Times.Once);
    }

    [Fact]
    public async Task RecordTrade_ReturnsOkWithPosition()
    {
        // Arrange
        var req = new TradeRequest(TradeType.Buy, 5, 160m, "Buying more");
        var pos = new PortfolioPositionResponse(1, "AAPL", "Apple", "NASDAQ", null, 15, 153.33m, 160m, 2400m, 2300m, 100m, 4.35, 2m, 1.25m, "Tech");
        _serviceMock.Setup(s => s.RecordTradeAsync("AAPL", req, TestUserId, Ct)).ReturnsAsync(pos);

        // Act
        var result = await _sut.RecordTrade("AAPL", req, Ct);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(pos);
    }

    [Fact]
    public async Task GetTrades_ReturnsOkWithTrades()
    {
        // Arrange
        var trades = new List<TradeResponse> { new(Guid.NewGuid(), "AAPL", TradeType.Buy, 10, 150m, null, DateTime.UtcNow) };
        _serviceMock.Setup(s => s.GetTradesBySymbolAsync("AAPL", TestUserId, Ct)).ReturnsAsync(trades);

        // Act
        var result = await _sut.GetTrades("AAPL", Ct);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(trades);
    }

    [Fact]
    public async Task RemovePosition_ReturnsNoContent()
    {
        // Act
        var result = await _sut.RemovePosition("AAPL", Ct);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _serviceMock.Verify(s => s.RemovePositionAsync("AAPL", TestUserId, Ct), Times.Once);
    }

    [Fact]
    public async Task GetAlerts_ReturnsOkWithAlerts()
    {
        // Arrange
        var alerts = new List<PortfolioAlertResponse> { new("AAPL", 160m, 150m, 0, DateTime.UtcNow) };
        _serviceMock.Setup(s => s.GetPortfolioAlertsAsync(TestUserId, Ct)).ReturnsAsync(alerts);

        // Act
        var result = await _sut.GetAlerts(Ct);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(alerts);
    }
}
