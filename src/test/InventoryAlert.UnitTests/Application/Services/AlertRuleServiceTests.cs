using FluentAssertions;
using InventoryAlert.Api.Services;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Application.Services;

public class AlertRuleServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IStockDataService> _stockDataService = new();
    private readonly AlertRuleService _sut;
    private static readonly string TestUserId = Guid.NewGuid().ToString();
    private static readonly CancellationToken Ct = CancellationToken.None;

    public AlertRuleServiceTests()
    {
        _sut = new AlertRuleService(_unitOfWork.Object, _stockDataService.Object);
        _unitOfWork
            .Setup(u => u.ExecuteTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((action, _) => action());
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenSymbolCannotBeResolved()
    {
        var request = new AlertRuleRequest("NEW", AlertCondition.PriceAbove, 100m, true);

        _stockDataService
            .Setup(s => s.GetProfileAsync("NEW", Ct))
            .ReturnsAsync((StockProfileResponse?)null);

        Func<Task> act = () => _sut.CreateAsync(request, TestUserId, Ct);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Symbol NEW could not be resolved.");
    }

    [Fact]
    public async Task CreateAlert_AddsRule_WhenSymbolResolves()
    {
        var request = new AlertRuleRequest("AAPL", AlertCondition.PriceAbove, 150m, false);

        _stockDataService
            .Setup(s => s.GetProfileAsync("AAPL", Ct))
            .ReturnsAsync(new StockProfileResponse("AAPL", "Apple", "NASDAQ", "USD", "US", "Tech", null, null,
                "https://apple.com", null));

        _unitOfWork.Setup(u => u.AlertRules.AddAsync(It.IsAny<AlertRule>(), Ct))
            .ReturnsAsync(new AlertRule { Id = Guid.NewGuid() });

        await _sut.CreateAsync(request, TestUserId, Ct);

        _unitOfWork.Verify(u => u.AlertRules.AddAsync(It.IsAny<AlertRule>(), Ct), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ReplacesAllFields_WhenRuleExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existing = new AlertRule
        {
            Id = id,
            UserId = Guid.Parse(TestUserId),
            TickerSymbol = "OLD",
            TargetValue = 100m
        };
        var request = new AlertRuleRequest("NEW", AlertCondition.PriceBelow, 200m, false);

        _unitOfWork.Setup(u => u.AlertRules.GetByIdAsync(id, Ct)).ReturnsAsync(existing);

        // Act
        var res = await _sut.UpdateAsync(id, request, TestUserId, Ct);

        // Assert
        res.TickerSymbol.Should().Be("NEW");
        res.Condition.Should().Be(AlertCondition.PriceBelow);
        res.TargetValue.Should().Be(200m);
        _unitOfWork.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsMappedRules()
    {
        // Arrange
        var rules = new List<AlertRule>
        {
            new() { Id = Guid.NewGuid(), UserId = Guid.Parse(TestUserId), TickerSymbol = "AAPL", TargetValue = 150m, IsActive = true }
        };
        _unitOfWork.Setup(u => u.AlertRules.GetByUserIdAsync(TestUserId, Ct)).ReturnsAsync(rules);

        // Act
        var result = await _sut.GetByUserIdAsync(TestUserId, Ct);

        // Assert
        result.Should().HaveCount(1);
        result.First().TickerSymbol.Should().Be("AAPL");
    }

    [Fact]
    public async Task ToggleAsync_UpdatesIsActive_WhenRuleExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var rule = new AlertRule { Id = id, UserId = Guid.Parse(TestUserId), IsActive = true };
        _unitOfWork.Setup(u => u.AlertRules.GetByIdAsync(id, Ct)).ReturnsAsync(rule);

        // Act
        var result = await _sut.ToggleAsync(id, false, TestUserId, Ct);

        // Assert
        result.IsActive.Should().BeFalse();
        _unitOfWork.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_DeletesRule_WhenExistsAndUserMatches()
    {
        // Arrange
        var id = Guid.NewGuid();
        var rule = new AlertRule { Id = id, UserId = Guid.Parse(TestUserId) };
        _unitOfWork.Setup(u => u.AlertRules.GetByIdAsync(id, Ct)).ReturnsAsync(rule);

        // Act
        var success = await _sut.DeleteAsync(id, TestUserId, Ct);

        // Assert
        success.Should().BeTrue();
        _unitOfWork.Verify(u => u.AlertRules.DeleteAsync(rule, Ct), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenRuleDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        _unitOfWork.Setup(u => u.AlertRules.GetByIdAsync(id, Ct)).ReturnsAsync((AlertRule?)null);

        // Act
        var success = await _sut.DeleteAsync(id, TestUserId, Ct);

        // Assert
        success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenTargetValueZero()
    {
        var request = new AlertRuleRequest("AAPL", AlertCondition.PriceAbove, 0m, true);
        Func<Task> act = () => _sut.CreateAsync(request, TestUserId, Ct);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenRuleNotFound()
    {
        var id = Guid.NewGuid();
        _unitOfWork.Setup(u => u.AlertRules.GetByIdAsync(id, Ct)).ReturnsAsync((AlertRule?)null);
        var request = new AlertRuleRequest("AAPL", AlertCondition.PriceAbove, 100m, true);

        Func<Task> act = () => _sut.UpdateAsync(id, request, TestUserId, Ct);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ToggleAsync_Throws_WhenRuleNotFound()
    {
        var id = Guid.NewGuid();
        _unitOfWork.Setup(u => u.AlertRules.GetByIdAsync(id, Ct)).ReturnsAsync((AlertRule?)null);

        Func<Task> act = () => _sut.ToggleAsync(id, true, TestUserId, Ct);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenSymbolEmpty()
    {
        var request = new AlertRuleRequest("", AlertCondition.PriceAbove, 100m, true);
        Func<Task> act = () => _sut.CreateAsync(request, TestUserId, Ct);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenTargetValueZeroOrSymbolEmpty()
    {
        var id = Guid.NewGuid();
        var request1 = new AlertRuleRequest("", AlertCondition.PriceAbove, 100m, true);
        var request2 = new AlertRuleRequest("AAPL", AlertCondition.PriceAbove, 0m, true);

        Func<Task> act1 = () => _sut.UpdateAsync(id, request1, TestUserId, Ct);
        Func<Task> act2 = () => _sut.UpdateAsync(id, request2, TestUserId, Ct);

        await act1.Should().ThrowAsync<InvalidOperationException>();
        await act2.Should().ThrowAsync<InvalidOperationException>();
    }
}
