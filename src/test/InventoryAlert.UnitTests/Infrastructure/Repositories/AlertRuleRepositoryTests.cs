using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryAlert.UnitTests.Infrastructure.Repositories;

public class AlertRuleRepositoryTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsUserAlertRules()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new AlertRuleRepository(context);
        var userId = Guid.NewGuid();

        var rule = new AlertRule
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TickerSymbol = "AAPL",
            Condition = AlertCondition.PriceAbove,
            TargetValue = 200m,
            IsActive = true
        };

        context.AlertRules.Add(rule);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetByUserIdAsync(userId.ToString(), CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("AAPL", result.First().TickerSymbol);
    }

    [Fact]
    public async Task GetBySymbolAsync_ReturnsActiveRulesOnly()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new AlertRuleRepository(context);
        var active = new AlertRule { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), TickerSymbol = "AAPL", Condition = AlertCondition.PriceAbove, TargetValue = 200m, IsActive = true };
        var inactive = new AlertRule { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), TickerSymbol = "AAPL", Condition = AlertCondition.PriceBelow, TargetValue = 100m, IsActive = false };

        context.AlertRules.AddRange(active, inactive);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetBySymbolAsync("AAPL", CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.True(result.First().IsActive);
    }

    [Fact]
    public async Task GetBySymbolsAsync_ReturnsMatchingActiveRules()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new AlertRuleRepository(context);
        var rule1 = new AlertRule { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), TickerSymbol = "AAPL", Condition = AlertCondition.PriceAbove, TargetValue = 200m, IsActive = true };
        var rule2 = new AlertRule { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), TickerSymbol = "GOOGL", Condition = AlertCondition.PriceAbove, TargetValue = 3000m, IsActive = true };

        context.AlertRules.AddRange(rule1, rule2);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetBySymbolsAsync(new[] { "AAPL", "GOOGL" }, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count());
    }
}
