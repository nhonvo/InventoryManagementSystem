using Hangfire;

namespace InventoryAlert.Worker.ScheduledJobs;

public class KeepAliveJob(IHttpClientFactory httpClientFactory, ILogger<KeepAliveJob> logger)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<KeepAliveJob> _logger = logger;

    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            // Ping API endpoint to prevent free tier sleep
            var response = await client.GetAsync("http://127.0.0.1:8080/healthz", ct);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[KeepAliveJob] App self-ping succeeded (HTTP {StatusCode}).", (int)response.StatusCode);
            }
            else
            {
                _logger.LogWarning("[KeepAliveJob] Self-ping returned HTTP {StatusCode}.", (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[KeepAliveJob] Self-ping failed.");
        }
    }
}
