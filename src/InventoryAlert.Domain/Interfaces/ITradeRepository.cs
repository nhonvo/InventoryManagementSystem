using InventoryAlert.Domain.Entities.Postgres;

namespace InventoryAlert.Domain.Interfaces;

public interface ITradeRepository : IGenericRepository<Trade>
{
    Task<IEnumerable<Trade>> GetByUserAndSymbolAsync(Guid userId, string symbol, CancellationToken ct);
    Task<IEnumerable<Trade>> GetByUserAndSymbolsAsync(Guid userId, IEnumerable<string> symbols, CancellationToken ct);

    /// <summary>
    /// Computes net holdings via SUM(Buy) - SUM(Sell).
    /// </summary>
    Task<decimal> GetNetHoldingsAsync(Guid userId, string symbol, CancellationToken ct);

    /// <summary>
    /// Gets paged distinct ticker symbols where the user has recorded trades.
    /// </summary>
    Task<(IEnumerable<string> Symbols, int TotalCount)> GetTradedSymbolsPagedAsync(Guid userId, int pageNumber, int pageSize, string? search, CancellationToken ct);
}
