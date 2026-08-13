using FluentAssertions;
using InventoryAlert.Infrastructure.Persistence.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryAlert.UnitTests.Infrastructure.Repositories;

public class UnitOfWorkTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task ExecuteSynchronizedAsync_ExecutesActionWithinLock()
    {
        // Arrange
        using var context = CreateDbContext();
        using var uow = new UnitOfWork(context);

        var executed = false;

        // Act
        var result = await uow.ExecuteSynchronizedAsync(async () =>
        {
            executed = true;
            return await Task.FromResult("OK");
        }, CancellationToken.None);

        // Assert
        executed.Should().BeTrue();
        result.Should().Be("OK");
    }

    [Fact]
    public async Task ExecuteSynchronizedAsync_VoidOverload_ExecutesAction()
    {
        // Arrange
        using var context = CreateDbContext();
        using var uow = new UnitOfWork(context);

        var executed = false;

        // Act
        await uow.ExecuteSynchronizedAsync(async () =>
        {
            executed = true;
            await Task.CompletedTask;
        }, CancellationToken.None);

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public void UnitOfWork_InitializesAllRepositories()
    {
        // Arrange
        using var context = CreateDbContext();
        using var uow = new UnitOfWork(context);

        // Assert
        uow.StockListings.Should().NotBeNull();
        uow.WatchlistItems.Should().NotBeNull();
        uow.PriceHistories.Should().NotBeNull();
        uow.AlertRules.Should().NotBeNull();
        uow.Users.Should().NotBeNull();
        uow.Trades.Should().NotBeNull();
        uow.Metrics.Should().NotBeNull();
        uow.Earnings.Should().NotBeNull();
        uow.Recommendations.Should().NotBeNull();
        uow.Insiders.Should().NotBeNull();
        uow.Notifications.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteTransactionAsync_ExecutesActionAndSaves()
    {
        // Arrange
        using var context = CreateDbContext();
        using var uow = new UnitOfWork(context);
        var ranAction = false;
        var ranTask = false;

        // Act
        await uow.ExecuteTransactionAsync(() => { ranAction = true; }, CancellationToken.None);
        await uow.ExecuteTransactionAsync(async () => { ranTask = true; await Task.CompletedTask; }, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);

        // Assert
        ranAction.Should().BeTrue();
        ranTask.Should().BeTrue();
    }
}
