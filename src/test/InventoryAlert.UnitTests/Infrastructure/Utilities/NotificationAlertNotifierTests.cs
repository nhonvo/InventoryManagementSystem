using InventoryAlert.Domain.Common.Constants;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Infrastructure.Hubs;
using InventoryAlert.Infrastructure.Utilities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Infrastructure.Utilities;

public class NotificationAlertNotifierTests
{
    private readonly Mock<IHubContext<NotificationHub, INotificationHub>> _hubContextMock = new();
    private readonly Mock<IHubClients<INotificationHub>> _hubClientsMock = new();
    private readonly Mock<INotificationHub> _clientMock = new();
    private readonly Mock<ILogger<NotificationAlertNotifier>> _loggerMock = new();

    [Fact]
    public async Task NotifyAsync_PushesSignalRNotificationToUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _hubContextMock.Setup(h => h.Clients).Returns(_hubClientsMock.Object);
        _hubClientsMock.Setup(c => c.User(userId.ToString())).Returns(_clientMock.Object);

        var notifier = new NotificationAlertNotifier(_hubContextMock.Object, _loggerMock.Object);
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Message = "Price Triggered",
            TickerSymbol = "AAPL",
            Type = NotificationType.Price,
            Severity = NotificationSeverity.Warning,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await notifier.NotifyAsync(notification, CancellationToken.None);

        // Assert
        _clientMock.Verify(c => c.ReceiveNotification(It.Is<NotificationResponse>(n => n.TickerSymbol == "AAPL" && n.Message == "Price Triggered")), Times.Once);
    }

    [Fact]
    public async Task NotifyAsync_CatchesAndLogsException_WhenSignalRFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _hubContextMock.Setup(h => h.Clients).Throws(new InvalidOperationException("SignalR exception"));
        var notifier = new NotificationAlertNotifier(_hubContextMock.Object, _loggerMock.Object);
        var notification = new Notification { UserId = userId, Message = "Fail" };

        // Act
        await notifier.NotifyAsync(notification, CancellationToken.None);

        // Assert - Exception is caught gracefully without rethrowing
        _hubContextMock.Verify(h => h.Clients, Times.Once);
    }
}
