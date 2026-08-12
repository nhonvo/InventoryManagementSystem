using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Worker.Models;

using Hangfire;

namespace InventoryAlert.Worker.ScheduledJobs;

[DisableConcurrentExecution(timeoutInSeconds: 300)]
[AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
public class SyncEarningsJob(
    IUnitOfWork unitOfWork,
    IFinnhubClient finnhub,
    ILogger<SyncEarningsJob> logger)
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
                    var surprise = await finnhub.GetEarningsAsync(listing.TickerSymbol, ct);
                    if (surprise == null || surprise.Count == 0) continue;

                    var data = surprise.Select(s => new EarningsSurprise
                    {
                        TickerSymbol = listing.TickerSymbol,
                        Period = DateOnly.TryParse(s.Period, out var p) ? p : DateOnly.MinValue,
                        ActualEps = s.Actual,
                        EstimateEps = s.Estimate,
                        SurprisePercent = s.SurprisePercent,
                        ReportDate = DateOnly.TryParse(s.ReportDate, out var rd) ? rd : null
                    });

                    await unitOfWork.Earnings.UpsertRangeAsync(data, ct);
                    count++;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "[SyncEarningsJob] Failed to sync earnings for {Symbol}: {Msg}", listing.TickerSymbol, ex.Message);
                }
            }

            await unitOfWork.SaveChangesAsync(ct);
            return new JobResult(JobStatus.Success, $"Earnings surprise sync completed for {count} symbols.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[SyncEarningsJob] Execution failure.");
            return new JobResult(JobStatus.Failed, "Failed to sync earnings.", Error: ex);
        }
    }
}
