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
}
