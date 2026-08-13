using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Worker.Models;
using InventoryAlert.Worker.ScheduledJobs;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Worker.ScheduledJobs;

public class CleanupPriceHistoryJobTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IPriceHistoryRepository> _priceHistoryRepoMock = new();
    private readonly Mock<ILogger<CleanupPriceHistoryJob>> _loggerMock = new();

    [Fact]
    public async Task ExecuteAsync_DeletesPriceHistoryOlderThanOneYear_ReturnsSuccess()
    {
        // Arrange
        _unitOfWorkMock.Setup(u => u.PriceHistories).Returns(_priceHistoryRepoMock.Object);
        _priceHistoryRepoMock
            .Setup(p => p.DeleteOlderThanAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var job = new CleanupPriceHistoryJob(_unitOfWorkMock.Object, _loggerMock.Object);

        // Act
        var result = await job.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.Equal(JobStatus.Success, result.Status);
        _priceHistoryRepoMock.Verify(p => p.DeleteOlderThanAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFailedStatus_WhenExceptionOccurs()
    {
        // Arrange
        _unitOfWorkMock.Setup(u => u.PriceHistories).Returns(_priceHistoryRepoMock.Object);
        _priceHistoryRepoMock
            .Setup(p => p.DeleteOlderThanAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var job = new CleanupPriceHistoryJob(_unitOfWorkMock.Object, _loggerMock.Object);

        // Act
        var result = await job.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.Equal(JobStatus.Failed, result.Status);
        Assert.NotNull(result.Error);
    }
}
