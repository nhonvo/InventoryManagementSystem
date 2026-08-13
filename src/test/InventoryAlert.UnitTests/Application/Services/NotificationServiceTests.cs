using FluentAssertions;
using InventoryAlert.Api.Services;
using InventoryAlert.Domain.Common.Constants;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Application.Services;

public class NotificationServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<INotificationRepository> _notificationRepoMock = new();
    private readonly NotificationService _sut;
    private static readonly Guid TestUserGuid = Guid.NewGuid();
    private static readonly string TestUserId = TestUserGuid.ToString();
    private static readonly CancellationToken Ct = CancellationToken.None;

    public NotificationServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Notifications).Returns(_notificationRepoMock.Object);
        _sut = new NotificationService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task CreateAsync_AddsNotificationAndSaves()
    {
        // Act
        var result = await _sut.CreateAsync(TestUserGuid, "Test Msg", NotificationType.Price, NotificationSeverity.Warning, "AAPL", null, Ct);

        // Assert
        result.Message.Should().Be("Test Msg");
        result.TickerSymbol.Should().Be("AAPL");
        result.Type.Should().Be(NotificationType.Price);
        result.Severity.Should().Be(NotificationSeverity.Warning);
        _notificationRepoMock.Verify(n => n.AddAsync(It.IsAny<Notification>(), Ct), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsMappedPagedResult()
    {
        // Arrange
        var notifications = new List<Notification>
        {
            new() { Id = Guid.NewGuid(), UserId = TestUserGuid, Message = "N1", IsRead = false, CreatedAt = DateTime.UtcNow }
        };
        var paged = new PagedResult<Notification> { Items = notifications, TotalItems = 1, PageNumber = 1, PageSize = 10 };
        _notificationRepoMock.Setup(n => n.GetByUserPagedAsync(TestUserId, false, 1, 10, Ct)).ReturnsAsync(paged);

        // Act
        var res = await _sut.GetPagedAsync(TestUserId, false, 1, 10, Ct);

        // Assert
        res.TotalItems.Should().Be(1);
        res.Items.First().Message.Should().Be("N1");
    }

    [Fact]
    public async Task GetUnreadCountAsync_ReturnsCountFromRepo()
    {
        // Arrange
        _notificationRepoMock.Setup(n => n.GetUnreadCountAsync(TestUserId, Ct)).ReturnsAsync(5);

        // Act
        var count = await _sut.GetUnreadCountAsync(TestUserId, Ct);

        // Assert
        count.Should().Be(5);
    }

    [Fact]
    public async Task MarkReadAsync_SetsIsReadTrue_WhenNotificationExistsAndUserMatches()
    {
        // Arrange
        var id = Guid.NewGuid();
        var notification = new Notification { Id = id, UserId = TestUserGuid, IsRead = false };
        _notificationRepoMock.Setup(n => n.GetByIdAsync(id, Ct)).ReturnsAsync(notification);

        // Act
        await _sut.MarkReadAsync(id, TestUserId, Ct);

        // Assert
        notification.IsRead.Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }

    [Fact]
    public async Task MarkAllReadAsync_CallsRepoMarkAllRead()
    {
        // Arrange
        _notificationRepoMock.Setup(n => n.MarkAllReadAsync(TestUserId, Ct)).ReturnsAsync(3);

        // Act
        var result = await _sut.MarkAllReadAsync(TestUserId, Ct);

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public async Task DismissAsync_DeletesNotification_WhenExistsAndUserMatches()
    {
        // Arrange
        var id = Guid.NewGuid();
        var notification = new Notification { Id = id, UserId = TestUserGuid };
        _notificationRepoMock.Setup(n => n.GetByIdAsync(id, Ct)).ReturnsAsync(notification);

        // Act
        await _sut.DismissAsync(id, TestUserId, Ct);

        // Assert
        _notificationRepoMock.Verify(n => n.DeleteAsync(notification, Ct), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }
}
