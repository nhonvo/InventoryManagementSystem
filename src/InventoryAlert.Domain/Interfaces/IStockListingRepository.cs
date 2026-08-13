using InventoryAlert.Domain.Entities.Postgres;

namespace InventoryAlert.Domain.Interfaces;

public interface IStockListingRepository : IGenericRepository<StockListing>
{
    Task<StockListing?> FindBySymbolAsync(string symbol, CancellationToken ct);
    Task<IEnumerable<StockListing>> FindBySymbolsAsync(IEnumerable<string> symbols, CancellationToken ct);
    Task<IEnumerable<StockListing>> SearchAsync(string query, CancellationToken ct);

    /// <summary>
    /// Returns distinct ticker symbols actively present in WatchlistItems, Trades, or AlertRules.
    /// </summary>
    Task<IEnumerable<string>> GetActiveSymbolsAsync(CancellationToken ct);
}
