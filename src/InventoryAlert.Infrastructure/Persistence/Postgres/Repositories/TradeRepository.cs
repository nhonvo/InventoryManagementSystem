using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;

public class TradeRepository(AppDbContext context)
    : GenericRepository<Trade>(context), ITradeRepository
{
    public async Task<IEnumerable<Trade>> GetByUserAndSymbolAsync(Guid userId, string symbol, CancellationToken ct)
    {
        return await _dbSet.AsNoTracking()
            .Where(x => x.UserId == userId && x.TickerSymbol == symbol)
            .OrderByDescending(x => x.TradedAt)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Trade>> GetByUserAndSymbolsAsync(Guid userId, IEnumerable<string> symbols, CancellationToken ct)
    {
        return await _dbSet.AsNoTracking()
            .Where(x => x.UserId == userId && symbols.Contains(x.TickerSymbol))
            .ToListAsync(ct);
    }

    public async Task<decimal> GetNetHoldingsAsync(Guid userId, string symbol, CancellationToken ct)
    {
        return await _dbSet.AsNoTracking()
            .Where(x => x.UserId == userId && x.TickerSymbol == symbol)
            .SumAsync(x => x.Type == TradeType.Buy ? x.Quantity : -x.Quantity, ct);
    }

    public async Task<(IEnumerable<string> Symbols, int TotalCount)> GetTradedSymbolsPagedAsync(Guid userId, int pageNumber, int pageSize, string? search, CancellationToken ct)
    {
        var query = _dbSet.AsNoTracking()
            .Where(x => x.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToUpperInvariant();
            query = query.Where(x => x.TickerSymbol.Contains(s));
        }

        var distinctSymbols = query.Select(x => x.TickerSymbol).Distinct();

        var totalCount = await distinctSymbols.CountAsync(ct);

        var items = await distinctSymbols
            .OrderBy(x => x)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
