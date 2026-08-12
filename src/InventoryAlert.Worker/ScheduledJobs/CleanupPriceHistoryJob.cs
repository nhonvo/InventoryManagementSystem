using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Worker.Models;

using Hangfire;

namespace InventoryAlert.Worker.ScheduledJobs;

[DisableConcurrentExecution(timeoutInSeconds: 300)]
[AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
public class CleanupPriceHistoryJob(
    IUnitOfWork unitOfWork,
    ILogger<CleanupPriceHistoryJob> logger)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<CleanupPriceHistoryJob> _logger = logger;

    public async Task<JobResult> ExecuteAsync(CancellationToken ct)
    {
        try
        {
            var cutoff = DateTime.UtcNow.AddYears(-1);
            _logger.LogInformation("[Cleanup] Deleting price history older than {Cutoff}", cutoff);

            await _unitOfWork.PriceHistories.DeleteOlderThanAsync(cutoff, ct);

            return new JobResult(JobStatus.Success, $"Cleanup finished. Deleted records older than {cutoff:O}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Cleanup] Error during price history cleanup.");
            return new JobResult(JobStatus.Failed, "Cleanup failure.", Error: ex);
        }
    }
}
