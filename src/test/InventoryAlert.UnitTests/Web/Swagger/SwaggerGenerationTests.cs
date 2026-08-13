using System.Net;
using InventoryAlert.Api.Extensions;
using InventoryAlert.Domain.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace InventoryAlert.UnitTests.Web.Swagger;

public class SwaggerGenerationTests
{
    [Fact]
    public async Task GetSwaggerJson_Returns200OK_WithValidJson()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var appSettings = new AppSettings();
        
        builder.Services.AddControllers()
            .AddApplicationPart(typeof(InventoryAlert.Api.Program).Assembly);
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerOpenAPI(appSettings);

        var app = builder.Build();
        app.UseSwaggerWithUI();
        app.MapControllers();

        await app.StartAsync();
        var client = app.GetTestClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("InventoryAlert API", content);
    }
}
