using FluentAssertions;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryAlert.UnitTests.Infrastructure.Repositories;

public class InsiderTransactionRepositoryTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetBySymbolAsync_ReturnsOrderedTransactions()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new InsiderTransactionRepository(context);
        context.InsiderTransactions.Add(new InsiderTransaction { TickerSymbol = "AAPL", Name = "Tim Cook", Share = 500 });
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetBySymbolAsync("AAPL", CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Tim Cook");
    }

    [Fact]
    public async Task ReplaceForSymbolAsync_ReplacesAllEntriesForSymbol()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new InsiderTransactionRepository(context);
        context.InsiderTransactions.Add(new InsiderTransaction { TickerSymbol = "AAPL", Name = "Old Name" });
        await context.SaveChangesAsync();

        var newEntries = new List<InsiderTransaction>
        {
            new() { TickerSymbol = "AAPL", Name = "New Name" }
        };

        // Act
        await repo.ReplaceForSymbolAsync("AAPL", newEntries, CancellationToken.None);
        await context.SaveChangesAsync();

        // Assert
        var remaining = await repo.GetBySymbolAsync("AAPL", CancellationToken.None);
        remaining.Should().HaveCount(1);
        remaining.First().Name.Should().Be("New Name");
    }
}
