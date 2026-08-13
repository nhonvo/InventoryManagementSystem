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
    public async Task GetBySymbolAsync_ReturnsTransactions_OrderedByDateDescending()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new InsiderTransactionRepository(context);

        var t1 = new InsiderTransaction { Id = 1, TickerSymbol = "AAPL", Name = "Tim Cook", Share = 1000, TransactionDate = new DateOnly(2026, 1, 1) };
        var t2 = new InsiderTransaction { Id = 2, TickerSymbol = "AAPL", Name = "Tim Cook", Share = 2000, TransactionDate = new DateOnly(2026, 2, 1) };

        context.InsiderTransactions.AddRange(t1, t2);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetBySymbolAsync("AAPL", CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Equal(new DateOnly(2026, 2, 1), result.First().TransactionDate);
    }
}
