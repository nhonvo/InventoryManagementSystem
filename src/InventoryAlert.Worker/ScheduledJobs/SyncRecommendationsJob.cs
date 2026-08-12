using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Worker.Models;

using Hangfire;

namespace InventoryAlert.Worker.ScheduledJobs;

[DisableConcurrentExecution(timeoutInSeconds: 300)]
[AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
public class SyncRecommendationsJob(
    IUnitOfWork unitOfWork,
    IFinnhubClient finnhub,
    ILogger<SyncRecommendationsJob> logger)
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
                    var trends = await finnhub.GetRecommendationsAsync(listing.TickerSymbol, ct);
                    if (trends == null || trends.Count == 0) continue;

                    var data = trends.Select(t => new RecommendationTrend
                    {
                        TickerSymbol = listing.TickerSymbol,
                        Period = t.Period ?? "N/A",
                        StrongBuy = t.StrongBuy,
                        Buy = t.Buy,
                        Hold = t.Hold,
                        Sell = t.Sell,
                        StrongSell = t.StrongSell
                    });

                    await unitOfWork.Recommendations.UpsertRangeAsync(data, ct);
                    count++;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "[SyncRecommendationsJob] Failed to sync recommendations for {Symbol}: {Msg}", listing.TickerSymbol, ex.Message);
                }
            }

            await unitOfWork.SaveChangesAsync(ct);
            return new JobResult(JobStatus.Success, $"Analyst recommendations sync completed for {count} symbols.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[SyncRecommendationsJob] Execution failure.");
            return new JobResult(JobStatus.Failed, "Failed to sync recommendations.", Error: ex);
        }
    }
}
