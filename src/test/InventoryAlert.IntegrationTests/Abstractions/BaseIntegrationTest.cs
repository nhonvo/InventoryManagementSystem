using System.Text.Json;
using System.Text.Json.Serialization;
using InventoryAlert.IntegrationTests.Config;
using InventoryAlert.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Configuration;
using RestSharp;
using RestSharp.Serializers.Json;
using Xunit.Abstractions;

namespace InventoryAlert.IntegrationTests.Abstractions;

[Collection("IntegrationTests")]
public abstract class BaseIntegrationTest : IAsyncLifetime
{
    protected readonly TestFixture Fixture;
    protected readonly ITestOutputHelper Output;
    protected readonly RestClient Client;
    protected readonly TestUser _testUser;

    protected BaseIntegrationTest(TestFixture fixture, ITestOutputHelper output)
    {
        Fixture = fixture;
        Output = output;

        // Use the in-process HttpClient from the factory
        var httpClient = Fixture.CreateTestClient();

        var options = new RestClientOptions
        {
            BaseUrl = new Uri(httpClient.BaseAddress!, "api/v1/")
        };

        Client = new RestClient(httpClient, options, configureSerialization: s => s.UseSystemTextJson(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        }));

        var appSettings = Fixture.Configuration.Get<AppSettings>();
        _testUser = appSettings?.TestUser ?? new TestUser { Username = "admin", Password = "password" };
    }

    public virtual async Task InitializeAsync()
    {
        await Fixture.ResetStateAsync();
    }

    public virtual Task DisposeAsync() => Task.CompletedTask;

    protected async Task<TestResult<T>> RunAction<T>(Func<Task<RestResponse<T>>> action)
    {
        return await Fixture.ApiActionConfig.RunActionAndViewLog(action);
    }
}
