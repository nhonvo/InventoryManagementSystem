using System.Security.Claims;
using FluentAssertions;
using InventoryAlert.Api.Controllers;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Web.Controllers;

public class WatchlistControllerTests
{
    private readonly Mock<IWatchlistService> _serviceMock = new();
    private readonly WatchlistController _sut;
    private static readonly string TestUserId = Guid.NewGuid().ToString();
    private static readonly CancellationToken Ct = CancellationToken.None;

    public WatchlistControllerTests()
    {
        _sut = new WatchlistController(_serviceMock.Object);

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
    public async Task GetWatchlist_ReturnsOkWithItems()
    {
        // Arrange
        var items = new List<PortfolioPositionResponse>
        {
            new(1, "AAPL", "Apple", "NASDAQ", null, 0, 0, 150m, 0, 0, 0, 0, 1m, 0.65m, "Tech")
        };
        _serviceMock.Setup(s => s.GetWatchlistAsync(TestUserId, Ct)).ReturnsAsync(items);

        // Act
        var result = await _sut.GetWatchlist(Ct);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(items);
    }

    [Fact]
    public async Task GetWatchlistItem_ReturnsOk_WhenExists()
    {
        // Arrange
        var item = new PortfolioPositionResponse(1, "AAPL", "Apple", "NASDAQ", null, 0, 0, 150m, 0, 0, 0, 0, 1m, 0.65m, "Tech");
        _serviceMock.Setup(s => s.GetWatchlistItemAsync("AAPL", TestUserId, Ct)).ReturnsAsync(item);

        // Act
        var result = await _sut.GetWatchlistItem("AAPL", Ct);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(item);
    }

    [Fact]
    public async Task GetWatchlistItem_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetWatchlistItemAsync("UNKNOWN", TestUserId, Ct)).ReturnsAsync((PortfolioPositionResponse?)null);

        // Act
        var result = await _sut.GetWatchlistItem("UNKNOWN", Ct);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AddToWatchlist_ReturnsCreatedAtAction_WhenAdded()
    {
        // Arrange
        var item = new PortfolioPositionResponse(1, "AAPL", "Apple", "NASDAQ", null, 0, 0, 150m, 0, 0, 0, 0, 1m, 0.65m, "Tech");
        _serviceMock.Setup(s => s.AddToWatchlistAsync("AAPL", TestUserId, Ct)).ReturnsAsync(item);

        // Act
        var result = await _sut.AddToWatchlist("AAPL", Ct);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.Value.Should().Be(item);
    }

    [Fact]
    public async Task AddToWatchlist_ReturnsBadRequest_WhenAlreadyOnWatchlist()
    {
        // Arrange
        _serviceMock.Setup(s => s.AddToWatchlistAsync("AAPL", TestUserId, Ct)).ReturnsAsync((PortfolioPositionResponse?)null);

        // Act
        var result = await _sut.AddToWatchlist("AAPL", Ct);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RemoveFromWatchlist_ReturnsOk()
    {
        // Act
        var result = await _sut.RemoveFromWatchlist("AAPL", Ct);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _serviceMock.Verify(s => s.RemoveFromWatchlistAsync("AAPL", TestUserId, Ct), Times.Once);
    }
}
