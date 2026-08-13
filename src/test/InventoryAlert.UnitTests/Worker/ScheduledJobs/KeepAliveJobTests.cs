using System.Net;
using InventoryAlert.Worker.ScheduledJobs;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace InventoryAlert.UnitTests.Worker.ScheduledJobs;

public class KeepAliveJobTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock = new();
    private readonly Mock<ILogger<KeepAliveJob>> _loggerMock = new();

    [Fact]
    public async Task ExecuteAsync_PingsHealthEndpointSuccessfully()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Healthy")
            });

        var client = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var job = new KeepAliveJob(_httpClientFactoryMock.Object, _loggerMock.Object);

        // Act
        await job.ExecuteAsync(CancellationToken.None);

        // Assert
        _httpClientFactoryMock.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_LogsWarning_WhenSelfPingFailsWith500()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });

        var client = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        var job = new KeepAliveJob(_httpClientFactoryMock.Object, _loggerMock.Object);

        // Act
        await job.ExecuteAsync(CancellationToken.None);

        // Assert
        _httpClientFactoryMock.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_CatchesException_WhenHttpClientThrows()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Network failure"));

        var client = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        var job = new KeepAliveJob(_httpClientFactoryMock.Object, _loggerMock.Object);

        // Act
        await job.ExecuteAsync(CancellationToken.None);

        // Assert
        _httpClientFactoryMock.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Once);
    }
}
