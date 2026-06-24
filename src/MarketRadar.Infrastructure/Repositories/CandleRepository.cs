using MarketRadar.Application.Contracts.Repositories;
using MarketRadar.Domain.Entities;
using MarketRadar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketRadar.Infrastructure.Repositories;

public sealed class CandleRepository : ICandleRepository
{
    private readonly MarketRadarDbContext _context;

    public CandleRepository(MarketRadarDbContext context) => _context = context;

    public async Task AddOrUpdateAsync(
        IReadOnlyCollection<MarketCandle> candles,
        CancellationToken cancellationToken)
    {
        var groups = candles.GroupBy(c => (c.Symbol, c.Timeframe));

        foreach (var group in groups)
        {
            var openTimes = group.Select(c => c.OpenTime).ToList();

            var existing = await _context.Candles
                .Where(c => c.Symbol == group.Key.Symbol
                         && c.Timeframe == group.Key.Timeframe
                         && openTimes.Contains(c.OpenTime))
                .ToDictionaryAsync(c => c.OpenTime, cancellationToken);

            foreach (var candle in group)
            {
                if (existing.TryGetValue(candle.OpenTime, out var row))
                {
                    row.OpenPrice  = candle.OpenPrice;
                    row.HighPrice  = candle.HighPrice;
                    row.LowPrice   = candle.LowPrice;
                    row.ClosePrice = candle.ClosePrice;
                    row.Volume     = candle.Volume;
                }
                else
                {
                    candle.CreatedAt = DateTime.UtcNow;
                    _context.Candles.Add(candle);
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<MarketCandle>> GetRecentAsync(
        string symbol,
        string timeframe,
        int count,
        CancellationToken cancellationToken)
    {
        var rows = await _context.Candles
            .Where(c => c.Symbol == symbol && c.Timeframe == timeframe)
            .OrderByDescending(c => c.OpenTime)
            .Take(count)
            .ToListAsync(cancellationToken);

        rows.Reverse();
        return rows.AsReadOnly();
    }

    public Task<int> CountAsync(string symbol, string timeframe, CancellationToken cancellationToken)
        => _context.Candles
            .Where(c => c.Symbol == symbol && c.Timeframe == timeframe)
            .CountAsync(cancellationToken);
}
