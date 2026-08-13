using InventoryAlert.Domain.Common.Constants;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryAlert.UnitTests.Infrastructure.Repositories;

public class NotificationRepositoryTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetByUserPagedAsync_ReturnsPagedNotifications()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new NotificationRepository(context);
        var userId = Guid.NewGuid();

        var n1 = new Notification { Id = Guid.NewGuid(), UserId = userId, Message = "Msg 1", Type = NotificationType.System, Severity = NotificationSeverity.Info, IsRead = false, CreatedAt = DateTime.UtcNow.AddMinutes(-5) };
        var n2 = new Notification { Id = Guid.NewGuid(), UserId = userId, Message = "Msg 2", Type = NotificationType.Price, Severity = NotificationSeverity.Warning, IsRead = true, CreatedAt = DateTime.UtcNow };

        context.Notifications.AddRange(n1, n2);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetByUserPagedAsync(userId.ToString(), onlyUnread: false, page: 1, pageSize: 10, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public async Task GetByUserPagedAsync_FiltersOnlyUnread_WhenOnlyUnreadIsTrue()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new NotificationRepository(context);
        var userId = Guid.NewGuid();

        var unread = new Notification { Id = Guid.NewGuid(), UserId = userId, Message = "Msg 1", Type = NotificationType.System, Severity = NotificationSeverity.Info, IsRead = false, CreatedAt = DateTime.UtcNow };
        var read = new Notification { Id = Guid.NewGuid(), UserId = userId, Message = "Msg 2", Type = NotificationType.Price, Severity = NotificationSeverity.Warning, IsRead = true, CreatedAt = DateTime.UtcNow };

        context.Notifications.AddRange(unread, read);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetByUserPagedAsync(userId.ToString(), onlyUnread: true, page: 1, pageSize: 10, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.TotalItems);
        Assert.False(result.Items.First().IsRead);
    }

    [Fact]
    public async Task GetUnreadCountAsync_ReturnsUnreadCount()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new NotificationRepository(context);
        var userId = Guid.NewGuid();

        context.Notifications.AddRange(
            new Notification { Id = Guid.NewGuid(), UserId = userId, Message = "U1", Type = NotificationType.System, Severity = NotificationSeverity.Info, IsRead = false, CreatedAt = DateTime.UtcNow },
            new Notification { Id = Guid.NewGuid(), UserId = userId, Message = "U2", Type = NotificationType.System, Severity = NotificationSeverity.Info, IsRead = false, CreatedAt = DateTime.UtcNow },
            new Notification { Id = Guid.NewGuid(), UserId = userId, Message = "R1", Type = NotificationType.System, Severity = NotificationSeverity.Info, IsRead = true, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        // Act
        var count = await repo.GetUnreadCountAsync(userId.ToString(), CancellationToken.None);

        // Assert
        Assert.Equal(2, count);
    }
}
