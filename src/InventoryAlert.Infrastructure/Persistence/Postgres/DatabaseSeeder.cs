using InventoryAlert.Domain.Entities.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventoryAlert.Infrastructure.Persistence.Postgres;

/// <summary>
/// Idempotent seed data initializer for Development and Docker environments.
/// Runs automatically on startup if the database is fresh.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext db, ILogger logger, CancellationToken ct = default)
    {
        if (!await db.Users.AnyAsync(ct))
        {
            logger.LogInformation("[Seeder] Seeding Users...");

            await db.Users.AddRangeAsync(
            [
                new User {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    Username = "admin",
                    Email = "admin@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new User
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Username = "user1",
                    Email = "user1@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
            ], ct);

            await db.SaveChangesAsync(ct);
            logger.LogInformation("[Seeder] Seeded users successfully.");
        }

        var seedListings = new[]
        {
            new StockListing { TickerSymbol = "AAPL", Name = "Apple Inc", Exchange = "NASDAQ", Currency = "USD", Country = "US", Industry = "Technology", MarketCap = 3450000 },
            new StockListing { TickerSymbol = "MSFT", Name = "Microsoft Corp", Exchange = "NASDAQ", Currency = "USD", Country = "US", Industry = "Technology", MarketCap = 3120000 },
            new StockListing { TickerSymbol = "GOOGL", Name = "Alphabet Inc", Exchange = "NASDAQ", Currency = "USD", Country = "US", Industry = "Technology", MarketCap = 2100000 },
            new StockListing { TickerSymbol = "AMZN", Name = "Amazon.com Inc", Exchange = "NASDAQ", Currency = "USD", Country = "US", Industry = "Consumer Cyclical", MarketCap = 2050000 },
            new StockListing { TickerSymbol = "NVDA", Name = "NVIDIA Corp", Exchange = "NASDAQ", Currency = "USD", Country = "US", Industry = "Technology", MarketCap = 3150000 },
            new StockListing { TickerSymbol = "META", Name = "Meta Platforms Inc", Exchange = "NASDAQ", Currency = "USD", Country = "US", Industry = "Technology", MarketCap = 1350000 },
            new StockListing { TickerSymbol = "TSLA", Name = "Tesla Inc", Exchange = "NASDAQ", Currency = "USD", Country = "US", Industry = "Automobiles", MarketCap = 1050000 },
            new StockListing { TickerSymbol = "JPM", Name = "JPMorgan Chase & Co", Exchange = "NYSE", Currency = "USD", Country = "US", Industry = "Financial Services", MarketCap = 580000 },
            new StockListing { TickerSymbol = "V", Name = "Visa Inc", Exchange = "NYSE", Currency = "USD", Country = "US", Industry = "Financial Services", MarketCap = 560000 },
            new StockListing { TickerSymbol = "UNH", Name = "UnitedHealth Group Inc", Exchange = "NYSE", Currency = "USD", Country = "US", Industry = "Healthcare", MarketCap = 530000 },
            new StockListing { TickerSymbol = "WMT", Name = "Walmart Inc", Exchange = "NYSE", Currency = "USD", Country = "US", Industry = "Consumer Defensive", MarketCap = 540000 },
            new StockListing { TickerSymbol = "PG", Name = "Procter & Gamble Co", Exchange = "NYSE", Currency = "USD", Country = "US", Industry = "Consumer Defensive", MarketCap = 400000 },
            new StockListing { TickerSymbol = "JNJ", Name = "Johnson & Johnson", Exchange = "NYSE", Currency = "USD", Country = "US", Industry = "Healthcare", MarketCap = 380000 },
            new StockListing { TickerSymbol = "XOM", Name = "Exxon Mobil Corp", Exchange = "NYSE", Currency = "USD", Country = "US", Industry = "Energy", MarketCap = 480000 },
            new StockListing { TickerSymbol = "MA", Name = "Mastercard Inc", Exchange = "NYSE", Currency = "USD", Country = "US", Industry = "Financial Services", MarketCap = 450000 },
            new StockListing { TickerSymbol = "HD", Name = "Home Depot Inc", Exchange = "NYSE", Currency = "USD", Country = "US", Industry = "Consumer Cyclical", MarketCap = 370000 },
            new StockListing { TickerSymbol = "BAC", Name = "Bank of America Corp", Exchange = "NYSE", Currency = "USD", Country = "US", Industry = "Financial Services", MarketCap = 310000 },
            new StockListing { TickerSymbol = "DIS", Name = "Walt Disney Co", Exchange = "NYSE", Currency = "USD", Country = "US", Industry = "Communication Services", MarketCap = 210000 },
            new StockListing { TickerSymbol = "NFLX", Name = "Netflix Inc", Exchange = "NASDAQ", Currency = "USD", Country = "US", Industry = "Communication Services", MarketCap = 280000 },
            new StockListing { TickerSymbol = "AMD", Name = "Advanced Micro Devices Inc", Exchange = "NASDAQ", Currency = "USD", Country = "US", Industry = "Technology", MarketCap = 250000 }
        };

        var existingDbListings = await db.StockListings.ToListAsync(ct);
        var existingSymbols = existingDbListings.Select(s => s.TickerSymbol).ToHashSet();

        // Update any existing listings with zero or null MarketCap
        var map = seedListings.ToDictionary(s => s.TickerSymbol, s => s.MarketCap);
        bool modified = false;
        foreach (var l in existingDbListings)
        {
            if (l.MarketCap == null || l.MarketCap == 0)
            {
                if (map.TryGetValue(l.TickerSymbol, out var mc))
                {
                    l.MarketCap = mc;
                    modified = true;
                }
                else if (l.TickerSymbol.Equals("TLSA", StringComparison.OrdinalIgnoreCase))
                {
                    l.MarketCap = 150;
                    modified = true;
                }
                else if (l.TickerSymbol.StartsWith("TSLA", StringComparison.OrdinalIgnoreCase))
                {
                    l.MarketCap = 1050000;
                    modified = true;
                }
                else
                {
                    l.MarketCap = 10000;
                    modified = true;
                }
            }
        }

        var missingListings = seedListings.Where(s => !existingSymbols.Contains(s.TickerSymbol)).ToList();
        if (missingListings.Count > 0)
        {
            logger.LogInformation("[Seeder] Adding {Count} missing StockListings...", missingListings.Count);
            await db.StockListings.AddRangeAsync(missingListings, ct);
            modified = true;
        }

        if (modified)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("[Seeder] Seeded/updated stock listings MarketCap successfully.");
        }
    }
}
