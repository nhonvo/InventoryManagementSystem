using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;

public class StockListingRepository(AppDbContext context)
    : GenericRepository<StockListing>(context), IStockListingRepository
{
    public async Task<StockListing?> FindBySymbolAsync(string symbol, CancellationToken ct)
    {
        return await _dbSet.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TickerSymbol == symbol, ct);
    }

    public async Task<IEnumerable<StockListing>> FindBySymbolsAsync(IEnumerable<string> symbols, CancellationToken ct)
    {
        return await _dbSet.AsNoTracking()
            .Where(x => symbols.Contains(x.TickerSymbol))
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<StockListing>> SearchAsync(string query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetAllAsync(ct);

        return await _dbSet.AsNoTracking()
            .Where(x => EF.Functions.ILike(x.Name, $"%{query}%") ||
                        EF.Functions.ILike(x.TickerSymbol, $"%{query}%"))
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<string>> GetActiveSymbolsAsync(CancellationToken ct)
    {
        var watchlistSymbols = await _context.WatchlistItems.AsNoTracking().Select(x => x.TickerSymbol).Distinct().ToListAsync(ct);
        var tradeSymbols = await _context.Trades.AsNoTracking().Select(x => x.TickerSymbol).Distinct().ToListAsync(ct);
        var alertSymbols = await _context.AlertRules.AsNoTracking().Where(r => r.IsActive).Select(x => x.TickerSymbol).Distinct().ToListAsync(ct);

        var activeSymbols = watchlistSymbols
            .Concat(tradeSymbols)
            .Concat(alertSymbols)
            .Distinct()
            .ToList();

        if (activeSymbols.Count == 0)
        {
            activeSymbols = await _dbSet.AsNoTracking().Select(x => x.TickerSymbol).Distinct().ToListAsync(ct);
        }

        return activeSymbols;
    }
}
