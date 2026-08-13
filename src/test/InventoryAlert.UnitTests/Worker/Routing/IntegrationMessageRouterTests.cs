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

    [Fact]
    public async Task ProcessAndAcknowledgeAsync_RoutesMarketPriceAlert()
    {
        // Arrange
        var router = new IntegrationMessageRouter(_rawHandlerMock.Object, _backgroundJobsMock.Object, _correlationProviderMock.Object, _settings, _loggerMock.Object);
        var payload = new MarketPriceAlertPayload { Symbol = "AAPL", NewPrice = 150m };
        var envelope = new EventEnvelope { MessageId = Guid.NewGuid().ToString(), EventType = EventTypes.MarketPriceAlert, Payload = JsonSerializer.Serialize(payload) };
        var message = new Message { MessageId = "msg-2", Body = JsonSerializer.Serialize(envelope) };

        // Act
        var result = await router.ProcessAndAcknowledgeAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ProcessAndAcknowledgeAsync_RoutesSyncMarketNewsRequested()
    {
        // Arrange
        var router = new IntegrationMessageRouter(_rawHandlerMock.Object, _backgroundJobsMock.Object, _correlationProviderMock.Object, _settings, _loggerMock.Object);
        var envelope = new EventEnvelope { MessageId = Guid.NewGuid().ToString(), EventType = EventTypes.SyncMarketNewsRequested };
        var message = new Message { MessageId = "msg-3", Body = JsonSerializer.Serialize(envelope) };

        // Act
        var result = await router.ProcessAndAcknowledgeAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ProcessAndAcknowledgeAsync_RoutesSyncPricesRequested()
    {
        // Arrange
        var router = new IntegrationMessageRouter(_rawHandlerMock.Object, _backgroundJobsMock.Object, _correlationProviderMock.Object, _settings, _loggerMock.Object);
        var envelope = new EventEnvelope { MessageId = Guid.NewGuid().ToString(), EventType = EventTypes.SyncPricesRequested };
        var message = new Message { MessageId = "msg-4", Body = JsonSerializer.Serialize(envelope) };

        // Act
        var result = await router.ProcessAndAcknowledgeAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ProcessAndAcknowledgeAsync_UnwrapsSnsMessage()
    {
        // Arrange
        var router = new IntegrationMessageRouter(_rawHandlerMock.Object, _backgroundJobsMock.Object, _correlationProviderMock.Object, _settings, _loggerMock.Object);
        var envelope = new EventEnvelope { MessageId = Guid.NewGuid().ToString(), EventType = EventTypes.SyncPricesRequested };
        var snsWrapper = new { Type = "Notification", Message = JsonSerializer.Serialize(envelope) };
        var message = new Message { MessageId = "sns-msg-1", Body = JsonSerializer.Serialize(snsWrapper) };

        // Act
        var result = await router.ProcessAndAcknowledgeAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ProcessAndAcknowledgeAsync_ReturnsFalse_WhenTestFailureRequested()
    {
        // Arrange
        var router = new IntegrationMessageRouter(_rawHandlerMock.Object, _backgroundJobsMock.Object, _correlationProviderMock.Object, _settings, _loggerMock.Object);
        var envelope = new EventEnvelope { MessageId = "fail-msg", EventType = EventTypes.TestFailureRequested };
        var message = new Message { MessageId = "msg-fail", Body = JsonSerializer.Serialize(envelope) };

        // Act
        var result = await router.ProcessAndAcknowledgeAsync(message, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ProcessAndAcknowledgeAsync_ReturnsTrue_WhenEventTypeIsEmpty()
    {
        // Arrange
        var router = new IntegrationMessageRouter(_rawHandlerMock.Object, _backgroundJobsMock.Object, _correlationProviderMock.Object, _settings, _loggerMock.Object);
        var envelope = new EventEnvelope { MessageId = "empty-msg", EventType = "" };
        var message = new Message { MessageId = "msg-empty", Body = JsonSerializer.Serialize(envelope) };

        // Act
        var result = await router.ProcessAndAcknowledgeAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ProcessMessageAsync_CallsProcessAndAcknowledgeAsync()
    {
        // Arrange
        var router = new IntegrationMessageRouter(_rawHandlerMock.Object, _backgroundJobsMock.Object, _correlationProviderMock.Object, _settings, _loggerMock.Object);
        var envelope = new EventEnvelope { MessageId = "alias-msg", EventType = EventTypes.SyncPricesRequested };
        var message = new Message { MessageId = "msg-alias", Body = JsonSerializer.Serialize(envelope) };

        // Act
        await router.ProcessMessageAsync(message, CancellationToken.None);

        // Assert
        _correlationProviderMock.Verify(c => c.SetCorrelationId(It.IsAny<string>()), Times.Once);
    }
}
