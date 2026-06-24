using MarketRadar.Domain.Entities;

namespace MarketRadar.Application.Contracts.Repositories;

public interface ICandleRepository
{
    Task AddOrUpdateAsync(
        IReadOnlyCollection<MarketCandle> candles,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<MarketCandle>> GetRecentAsync(
        string symbol,
        string timeframe,
        int count,
        CancellationToken cancellationToken);

    Task<int> CountAsync(
        string symbol,
        string timeframe,
        CancellationToken cancellationToken);
}
