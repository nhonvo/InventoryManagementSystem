using Amazon.SQS.Model;
using InventoryAlert.Domain.Configuration;
using InventoryAlert.Worker.Configuration;
using InventoryAlert.Worker.IntegrationEvents.Handlers;
using InventoryAlert.Worker.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Worker.Handlers;

public class DefaultHandlerTests
{
    private readonly Mock<ISqsHelper> _sqsHelperMock = new();
    private readonly Mock<ILogger<DefaultHandler>> _loggerMock = new();
    private readonly WorkerSettings _settings = new() { Aws = new SharedAwsSettings { SqsQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue" } };

    [Fact]
    public async Task HandleAsync_DeletesMessageFromSqsQueue()
    {
        // Arrange
        _sqsHelperMock
            .Setup(s => s.DeleteMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new DefaultHandler(_sqsHelperMock.Object, _loggerMock.Object, _settings);
        var msg = new Message { MessageId = "msg-123", ReceiptHandle = "handle-456" };

        // Act
        await handler.HandleAsync(msg, CancellationToken.None);

        // Assert
        _sqsHelperMock.Verify(s => s.DeleteMessageAsync(_settings.Aws.SqsQueueUrl, "handle-456", It.IsAny<CancellationToken>()), Times.Once);
    }
}
