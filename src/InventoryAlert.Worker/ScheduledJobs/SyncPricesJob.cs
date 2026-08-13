using System.Collections.Concurrent;
using InventoryAlert.Domain.Common.Constants;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Worker.Configuration;
using InventoryAlert.Worker.Models;

using Hangfire;

namespace InventoryAlert.Worker.ScheduledJobs;

[DisableConcurrentExecution(timeoutInSeconds: 300)]
[AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
public class SyncPricesJob(
    IUnitOfWork unitOfWork,
    IFinnhubClient finnhub,
    IAlertNotifier notifier,
    IAlertRuleEvaluator evaluator,
    WorkerSettings settings,
    ILogger<SyncPricesJob> logger)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IFinnhubClient _finnhub = finnhub;
    private readonly IAlertNotifier _notifier = notifier;
    private readonly IAlertRuleEvaluator _evaluator = evaluator;
    private readonly WorkerSettings _settings = settings;
    private readonly ILogger<SyncPricesJob> _logger = logger;

    public async Task<JobResult> ExecuteAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("[SyncPrices] Starting price synchronization...");

            var activeSymbols = await _unitOfWork.StockListings.GetActiveSymbolsAsync(ct);
            int success = 0;

            var fetchedQuotes = new ConcurrentDictionary<string, decimal>();
            var newPriceHistories = new ConcurrentBag<PriceHistory>();
            var pendingNotifications = new List<Notification>();

            // PART 1: Sync Price (Scoped to Active User Symbols & Throttled)
            var options = new ParallelOptions { MaxDegreeOfParallelism = Math.Min(_settings.MaxDegreeOfParallelism, 4), CancellationToken = ct };
            await Parallel.ForEachAsync(activeSymbols, options, async (symbol, token) =>
            {
                try
                {
                    var quote = await _finnhub.GetQuoteAsync(symbol, token);
                    if (quote?.CurrentPrice is null or 0) return;

                    newPriceHistories.Add(new PriceHistory
                    {
                        TickerSymbol = symbol,
                        RecordedAt = DateTime.UtcNow,
                        Open = quote.OpenPrice ?? 0,
                        High = quote.HighPrice ?? 0,
                        Low = quote.LowPrice ?? 0,
                        Price = quote.CurrentPrice.Value
                    });

                    fetchedQuotes[symbol] = quote.CurrentPrice.Value;
                    Interlocked.Increment(ref success);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("[SyncPrices] Failed to fetch quote for {Symbol}: {Msg}", symbol, ex.Message);
                }
            });

            if (!newPriceHistories.IsEmpty)
            {
                await _unitOfWork.PriceHistories.AddRangeAsync(newPriceHistories, ct);
            }

            // PART 2: Check Alerts (Unified Evaluator)
            var symbolsToProcess = fetchedQuotes.Keys.ToList();
            var allActiveRules = await _unitOfWork.AlertRules.GetBySymbolsAsync(symbolsToProcess, ct);
            var rulesBySymbol = allActiveRules.GroupBy(r => r.TickerSymbol).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var kvp in fetchedQuotes)
            {
                var symbol = kvp.Key;
                var currentPrice = kvp.Value;

                if (!rulesBySymbol.TryGetValue(symbol, out var rules)) continue;

                foreach (var rule in rules)
                {
                    var (isBreached, message) = await _evaluator.EvaluateAsync(rule, currentPrice, ct);

                    if (isBreached)
                    {
                        var notification = new Notification
                        {
                            UserId = rule.UserId,
                            Message = message,
                            TickerSymbol = symbol,
                            AlertRuleId = rule.Id,
                            Type = NotificationType.Price,
                            Severity = NotificationSeverity.Warning,
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        };

                        pendingNotifications.Add(notification);

                        if (rule.TriggerOnce) rule.IsActive = false;
                        rule.LastTriggeredAt = DateTime.UtcNow;
                    }
                }
            }

            // PART 3: Notify
            if (pendingNotifications.Count > 0)
            {
                await _unitOfWork.Notifications.AddRangeAsync(pendingNotifications, ct);

                foreach (var notification in pendingNotifications)
                {
                    await _notifier.NotifyAsync(notification, ct);
                }
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return new JobResult(JobStatus.Success, $"Synced {success} symbol prices and processed alerts.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SyncPrices] Execution failure.");
            return new JobResult(JobStatus.Failed, "Critical failure in price sync engine.", Error: ex);
        }
    }
}
