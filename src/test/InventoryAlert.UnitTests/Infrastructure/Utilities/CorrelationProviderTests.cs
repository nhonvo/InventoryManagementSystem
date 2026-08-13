using InventoryAlert.Infrastructure.Utilities;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Infrastructure.Utilities;

public class CorrelationProviderTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();

    [Fact]
    public void GetCorrelationId_ReturnsNA_WhenNoCorrelationIdSetOrInContext()
    {
        // Arrange
        _httpContextAccessorMock.Setup(h => h.HttpContext).Returns((HttpContext?)null);
        var provider = new CorrelationProvider(_httpContextAccessorMock.Object);

        // Act
        var cid = provider.GetCorrelationId();

        // Assert
        Assert.Equal("N/A", cid);
    }

    [Fact]
    public void SetCorrelationId_OverridesCorrelationId()
    {
        // Arrange
        var provider = new CorrelationProvider(_httpContextAccessorMock.Object);
        var testCid = "test-cid-123";

        // Act
        provider.SetCorrelationId(testCid);
        var cid = provider.GetCorrelationId();

        // Assert
        Assert.Equal(testCid, cid);
    }

    [Fact]
    public void GetCorrelationId_ReturnsFromHttpContextItems_WhenAvailable()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Items["X-Correlation-Id"] = "http-cid-456";
        _httpContextAccessorMock.Setup(h => h.HttpContext).Returns(httpContext);

        var provider = new CorrelationProvider(_httpContextAccessorMock.Object);
        provider.SetCorrelationId(string.Empty);

        // Act
        var cid = provider.GetCorrelationId();

        // Assert
        Assert.Equal("http-cid-456", cid);
    }
}
