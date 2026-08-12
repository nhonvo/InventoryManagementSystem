using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Worker.Models;

using Hangfire;

namespace InventoryAlert.Worker.ScheduledJobs;

[DisableConcurrentExecution(timeoutInSeconds: 300)]
[AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
public class SyncInsidersJob(
    IUnitOfWork unitOfWork,
    IFinnhubClient finnhub,
    ILogger<SyncInsidersJob> logger)
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
                    var response = await finnhub.GetInsidersAsync(listing.TickerSymbol, ct);
                    if (response == null || response.Data.Count == 0) continue;

                    var data = response.Data
                        .OrderByDescending(x => x.FilingDate)
                        .Take(100)
                        .Select(i => new InsiderTransaction
                        {
                            TickerSymbol = listing.TickerSymbol,
                            Name = i.Name,
                            Share = i.Share,
                            Value = (decimal?)(i.Share * i.TransactionPrice), // Estimated value
                            TransactionDate = DateOnly.TryParse(i.TransactionDate, out var td) ? td : null,
                            FilingDate = DateOnly.TryParse(i.FilingDate, out var fd) ? fd : null,
                            TransactionCode = i.TransactionCode
                        });

                    await unitOfWork.Insiders.ReplaceForSymbolAsync(listing.TickerSymbol, data, ct);
                    count++;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "[SyncInsidersJob] Failed to sync insiders for {Symbol}: {Msg}", listing.TickerSymbol, ex.Message);
                }
            }

            await unitOfWork.SaveChangesAsync(ct);
            return new JobResult(JobStatus.Success, $"Insider transactions sync completed for {count} symbols.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[SyncInsidersJob] Execution failure.");
            return new JobResult(JobStatus.Failed, "Failed to sync insiders.", Error: ex);
        }
    }
}
