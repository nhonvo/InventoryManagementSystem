using Hangfire;
using InventoryAlert.Worker.Configuration;
using InventoryAlert.Worker.ScheduledJobs;

namespace InventoryAlert.Worker.Hosting;

public sealed class JobSchedulerService(
    IRecurringJobManager recurringJobs,
    WorkerSettings settings,
    ILogger<JobSchedulerService> logger) : IHostedService
{
    private readonly IRecurringJobManager _recurringJobs = recurringJobs;
    private readonly WorkerSettings _settings = settings;
    private readonly ILogger _logger = logger;

    public Task StartAsync(CancellationToken ct)
    {
        var s = _settings.Schedules;

        _recurringJobs.AddOrUpdate<SyncPricesJob>(
            "sync-prices",
            x => x.ExecuteAsync(CancellationToken.None),
            s.SyncPrices);

        _recurringJobs.AddOrUpdate<SyncStockFundamentalsJob>(
            "sync-fundamentals",
            x => x.ExecuteAsync(CancellationToken.None),
            "10 6 * * *");

        _recurringJobs.AddOrUpdate<NewsSyncJob>(
            "news-sync",
            x => x.ExecuteAsync(CancellationToken.None),
            s.MarketNews);

        _recurringJobs.AddOrUpdate<CleanupPriceHistoryJob>(
            "cleanup-prices",
            x => x.ExecuteAsync(CancellationToken.None),
            "20 2 * * *");

        _recurringJobs.AddOrUpdate<KeepAliveJob>(
            "keep-alive",
            x => x.ExecuteAsync(CancellationToken.None),
            s.KeepAlive);

        _logger.LogInformation("[JobSchedulerService] All intelligence and cleanup jobs registered.");

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
