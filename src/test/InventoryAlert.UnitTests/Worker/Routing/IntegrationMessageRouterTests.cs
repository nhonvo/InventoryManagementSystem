using System.Text.Json;
using Amazon.SQS.Model;
using Hangfire;
using InventoryAlert.Domain.Configuration;
using InventoryAlert.Domain.Events;
using InventoryAlert.Domain.Events.Payloads;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Worker.IntegrationEvents.Handlers;
using InventoryAlert.Worker.IntegrationEvents.Routing;
using InventoryAlert.Worker.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Worker.Routing;

public class IntegrationMessageRouterTests
{
    private readonly Mock<IRawDefaultHandler> _rawHandlerMock = new();
    private readonly Mock<IBackgroundJobClient> _backgroundJobsMock = new();
    private readonly Mock<ICorrelationProvider> _correlationProviderMock = new();
    private readonly Mock<ILogger<IntegrationMessageRouter>> _loggerMock = new();
    private readonly AppSettings _settings = new();

    [Fact]
    public async Task ProcessAndAcknowledgeAsync_RoutesValidEnvelopeToHangfire()
    {
        // Arrange
        var router = new IntegrationMessageRouter(
            _rawHandlerMock.Object,
            _backgroundJobsMock.Object,
            _correlationProviderMock.Object,
            _settings,
            _loggerMock.Object);

        var payload = new LowHoldingsAlertPayload(Guid.NewGuid(), "AAPL", 5m, 10m);
        var envelope = new EventEnvelope
        {
            MessageId = Guid.NewGuid().ToString(),
            EventType = EventTypes.StockLowAlert,
            Payload = JsonSerializer.Serialize(payload),
            CorrelationId = "test-corr-id"
        };

        var message = new Message
        {
            MessageId = "msg-1",
            Body = JsonSerializer.Serialize(envelope)
        };

        // Act
        var result = await router.ProcessAndAcknowledgeAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result);
        _correlationProviderMock.Verify(c => c.SetCorrelationId("test-corr-id"), Times.Once);
    }

    [Fact]
    public async Task ProcessAndAcknowledgeAsync_DelegatesToRawHandler_WhenNonEnvelopeJson()
    {
        // Arrange
        var router = new IntegrationMessageRouter(
            _rawHandlerMock.Object,
            _backgroundJobsMock.Object,
            _correlationProviderMock.Object,
            _settings,
            _loggerMock.Object);

        var message = new Message
        {
            MessageId = "raw-msg-1",
            Body = "{ \"unknownProperty\": 123 }"
        };

        // Act
        var result = await router.ProcessAndAcknowledgeAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result);
        _rawHandlerMock.Verify(r => r.HandleAsync(message, It.IsAny<CancellationToken>()), Times.Once);
    }
}
