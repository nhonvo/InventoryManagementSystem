using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Worker.Models;

using Hangfire;

namespace InventoryAlert.Worker.ScheduledJobs;

[DisableConcurrentExecution(timeoutInSeconds: 300)]
[AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
public class SyncMetricsJob(
    IUnitOfWork unitOfWork,
    IFinnhubClient finnhub,
    ILogger<SyncMetricsJob> logger)
{
    public async Task<JobResult> ExecuteAsync(CancellationToken ct)
    {
        try
        {
            var listings = await unitOfWork.StockListings.GetAllAsync(ct);
            int count = 0;
            foreach (var listing in listings)
            {
                try
                {
                    var raw = await finnhub.GetMetricsAsync(listing.TickerSymbol, ct);
                    if (raw?.Metric == null) continue;

                    var m = raw.Metric;
                    var metric = new StockMetric
                    {
                        TickerSymbol = listing.TickerSymbol,
                        PeRatio = m.GetValueOrDefault("peExclExtraTTM"),
                        PbRatio = m.GetValueOrDefault("pbAnnual"),
                        EpsBasicTtm = m.GetValueOrDefault("epsBasicExclExtraItemsTTM"),
                        DividendYield = m.GetValueOrDefault("dividendYieldIndicatedAnnual"),
                        Week52High = (decimal)m.GetValueOrDefault("52WeekHigh"),
                        Week52Low = (decimal)m.GetValueOrDefault("52WeekLow"),
                        RevenueGrowthTtm = m.GetValueOrDefault("revenueGrowthTTMYoy"),
                        MarginNet = m.GetValueOrDefault("netProfitMarginTTM"),
                        LastSyncedAt = DateTime.UtcNow
                    };

                    await unitOfWork.Metrics.UpsertAsync(metric, ct);
                    count++;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "[SyncMetricsJob] Failed to sync metrics for {Symbol}: {Msg}", listing.TickerSymbol, ex.Message);
                }
            }

            await unitOfWork.SaveChangesAsync(ct);
            return new JobResult(JobStatus.Success, $"Financial metrics sync completed for {count} symbols.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[SyncMetricsJob] Execution failure.");
            return new JobResult(JobStatus.Failed, "Failed to sync metrics.", Error: ex);
        }
    }
}
