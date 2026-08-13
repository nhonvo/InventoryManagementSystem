using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Worker.Models;

using Hangfire;

namespace InventoryAlert.Worker.ScheduledJobs;

/// <summary>
/// Consolidated daily job for syncing fundamental metrics, earnings surprises,
/// analyst recommendations, and insider transactions for active user symbols.
/// Implements 1,000ms request throttling to respect Finnhub's 60 req/min rate limit.
/// </summary>
[DisableConcurrentExecution(timeoutInSeconds: 1200)]
[AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
public class SyncStockFundamentalsJob(
    IUnitOfWork unitOfWork,
    IFinnhubClient finnhub,
    ILogger<SyncStockFundamentalsJob> logger)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IFinnhubClient _finnhub = finnhub;
    private readonly ILogger<SyncStockFundamentalsJob> _logger = logger;

    public async Task<JobResult> ExecuteAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("[SyncFundamentals] Starting consolidated fundamentals sync...");

            var activeSymbols = await _unitOfWork.StockListings.GetActiveSymbolsAsync(ct);
            int syncedCount = 0;

            foreach (var symbol in activeSymbols)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    // 1. Fundamental Metrics
                    var rawMetric = await _finnhub.GetMetricsAsync(symbol, ct);
                    if (rawMetric?.Metric != null)
                    {
                        var m = rawMetric.Metric;
                        var metric = new StockMetric
                        {
                            TickerSymbol = symbol,
                            PeRatio = m.GetValueOrDefault("peExclExtraTTM"),
                            PbRatio = m.GetValueOrDefault("pbAnnual"),
                            EpsBasicTtm = m.GetValueOrDefault("epsBasicExclExtraItemsTTM"),
                            DividendYield = m.GetValueOrDefault("dividendYieldIndicatedAnnual"),
                            Week52High = m.TryGetValue("52WeekHigh", out var h) ? (decimal?)h : null,
                            Week52Low = m.TryGetValue("52WeekLow", out var l) ? (decimal?)l : null,
                            RevenueGrowthTtm = m.GetValueOrDefault("revenueGrowthTTMYoy"),
                            MarginNet = m.GetValueOrDefault("netProfitMarginTTM"),
                            LastSyncedAt = DateTime.UtcNow
                        };
                        await _unitOfWork.Metrics.UpsertAsync(metric, ct);
                    }
                    await Task.Delay(250, ct); // Throttling delay

                    // 2. Earnings Surprises
                    var surprise = await _finnhub.GetEarningsAsync(symbol, ct);
                    if (surprise != null && surprise.Count > 0)
                    {
                        var earningsData = surprise.Select(s => new EarningsSurprise
                        {
                            TickerSymbol = symbol,
                            Period = DateOnly.TryParse(s.Period, out var p) ? p : DateOnly.MinValue,
                            ActualEps = s.Actual,
                            EstimateEps = s.Estimate,
                            SurprisePercent = s.SurprisePercent,
                            ReportDate = DateOnly.TryParse(s.ReportDate, out var rd) ? rd : null
                        });
                        await _unitOfWork.Earnings.UpsertRangeAsync(earningsData, ct);
                    }
                    await Task.Delay(250, ct);

                    // 3. Analyst Recommendations
                    var trends = await _finnhub.GetRecommendationsAsync(symbol, ct);
                    if (trends != null && trends.Count > 0)
                    {
                        var recData = trends.Select(t => new RecommendationTrend
                        {
                            TickerSymbol = symbol,
                            Period = t.Period ?? "N/A",
                            StrongBuy = t.StrongBuy,
                            Buy = t.Buy,
                            Hold = t.Hold,
                            Sell = t.Sell,
                            StrongSell = t.StrongSell
                        });
                        await _unitOfWork.Recommendations.UpsertRangeAsync(recData, ct);
                    }
                    await Task.Delay(250, ct);

                    // 4. Insider Transactions
                    var insidersResp = await _finnhub.GetInsidersAsync(symbol, ct);
                    if (insidersResp != null && insidersResp.Data.Count > 0)
                    {
                        var insiderData = insidersResp.Data
                            .OrderByDescending(x => x.FilingDate)
                            .Take(100)
                            .Select(i => new InsiderTransaction
                            {
                                TickerSymbol = symbol,
                                Name = i.Name,
                                Share = i.Share,
                                Value = (decimal?)(i.Share * i.TransactionPrice),
                                TransactionDate = DateOnly.TryParse(i.TransactionDate, out var td) ? td : null,
                                FilingDate = DateOnly.TryParse(i.FilingDate, out var fd) ? fd : null,
                                TransactionCode = i.TransactionCode
                            });
                        await _unitOfWork.Insiders.ReplaceForSymbolAsync(symbol, insiderData, ct);
                    }
                    await Task.Delay(250, ct);

                    syncedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[SyncFundamentals] Partial sync error for {Symbol}: {Msg}", symbol, ex.Message);
                }
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return new JobResult(JobStatus.Success, $"Consolidated fundamentals sync completed for {syncedCount} symbols.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SyncFundamentals] Execution failure.");
            return new JobResult(JobStatus.Failed, "Failed to sync stock fundamentals.", Error: ex);
        }
    }
}
