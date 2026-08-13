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

public class NotificationsControllerTests
{
    private readonly Mock<INotificationService> _serviceMock = new();
    private readonly Mock<IAlertNotifier> _notifierMock = new();
    private readonly NotificationsController _sut;
    private static readonly Guid TestUserGuid = Guid.NewGuid();
    private static readonly string TestUserId = TestUserGuid.ToString();
    private static readonly CancellationToken Ct = CancellationToken.None;

    public NotificationsControllerTests()
    {
        _sut = new NotificationsController(_serviceMock.Object, _notifierMock.Object);

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
    public async Task TestSignalR_SendsNotificationAndReturnsOk()
    {
        // Act
        var result = await _sut.TestSignalR("Hello", Ct);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _notifierMock.Verify(n => n.NotifyAsync(It.IsAny<Notification>(), Ct), Times.Once);
    }

    [Fact]
    public async Task GetNotifications_ReturnsOkWithPagedResult()
    {
        // Arrange
        var paged = new PagedResult<NotificationResponse> { Items = [], TotalItems = 0, PageNumber = 1, PageSize = 20 };
        _serviceMock.Setup(s => s.GetPagedAsync(TestUserId, false, 1, 20, Ct)).ReturnsAsync(paged);

        // Act
        var result = await _sut.GetNotifications(false, 1, 20, Ct);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(paged);
    }

    [Fact]
    public async Task GetUnreadCount_ReturnsOkWithCount()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetUnreadCountAsync(TestUserId, Ct)).ReturnsAsync(3);

        // Act
        var result = await _sut.GetUnreadCount(Ct);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(3);
    }

    [Fact]
    public async Task MarkRead_ReturnsNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = await _sut.MarkRead(id, Ct);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _serviceMock.Verify(s => s.MarkReadAsync(id, TestUserId, Ct), Times.Once);
    }

    [Fact]
    public async Task MarkAllRead_ReturnsOkWithCount()
    {
        // Arrange
        _serviceMock.Setup(s => s.MarkAllReadAsync(TestUserId, Ct)).ReturnsAsync(5);

        // Act
        var result = await _sut.MarkAllRead(Ct);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(5);
    }

    [Fact]
    public async Task Dismiss_ReturnsNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = await _sut.Dismiss(id, Ct);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _serviceMock.Verify(s => s.DismissAsync(id, TestUserId, Ct), Times.Once);
    }
}
