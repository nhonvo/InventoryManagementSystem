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

public class AlertRulesControllerTests
{
    private readonly Mock<IAlertRuleService> _serviceMock = new();
    private readonly AlertRulesController _sut;
    private static readonly string TestUserId = Guid.NewGuid().ToString();
    private static readonly CancellationToken Ct = CancellationToken.None;

    public AlertRulesControllerTests()
    {
        _sut = new AlertRulesController(_serviceMock.Object);

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
    public async Task GetAlerts_ReturnsOkWithList()
    {
        // Arrange
        var rules = new List<AlertRuleResponse>
        {
            new(Guid.NewGuid(), "AAPL", AlertCondition.PriceAbove, 150m, true, false, null)
        };
        _serviceMock.Setup(s => s.GetByUserIdAsync(TestUserId, Ct)).ReturnsAsync(rules);

        // Act
        var result = await _sut.GetAlerts(Ct);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(rules);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction()
    {
        // Arrange
        var req = new AlertRuleRequest("AAPL", AlertCondition.PriceAbove, 150m, true);
        var rule = new AlertRuleResponse(Guid.NewGuid(), "AAPL", AlertCondition.PriceAbove, 150m, true, false, null);
        _serviceMock.Setup(s => s.CreateAsync(req, TestUserId, Ct)).ReturnsAsync(rule);

        // Act
        var result = await _sut.Create(req, Ct);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.Value.Should().Be(rule);
    }

    [Fact]
    public async Task Update_ReturnsOkWithUpdatedRule()
    {
        // Arrange
        var id = Guid.NewGuid();
        var req = new AlertRuleRequest("AAPL", AlertCondition.PriceBelow, 140m, true);
        var rule = new AlertRuleResponse(id, "AAPL", AlertCondition.PriceBelow, 140m, true, false, null);
        _serviceMock.Setup(s => s.UpdateAsync(id, req, TestUserId, Ct)).ReturnsAsync(rule);

        // Act
        var result = await _sut.Update(id, req, Ct);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(rule);
    }

    [Fact]
    public async Task Toggle_ReturnsOkWithToggledRule()
    {
        // Arrange
        var id = Guid.NewGuid();
        var req = new ToggleAlertRequest(false);
        var rule = new AlertRuleResponse(id, "AAPL", AlertCondition.PriceAbove, 150m, false, false, null);
        _serviceMock.Setup(s => s.ToggleAsync(id, false, TestUserId, Ct)).ReturnsAsync(rule);

        // Act
        var result = await _sut.Toggle(id, req, Ct);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(rule);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.DeleteAsync(id, TestUserId, Ct)).ReturnsAsync(true);

        // Act
        var result = await _sut.Delete(id, Ct);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenUnsuccessful()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.DeleteAsync(id, TestUserId, Ct)).ReturnsAsync(false);

        // Act
        var result = await _sut.Delete(id, Ct);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }
}
