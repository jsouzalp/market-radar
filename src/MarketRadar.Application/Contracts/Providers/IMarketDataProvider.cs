using MarketRadar.Domain.Entities;

namespace MarketRadar.Application.Contracts.Providers;

public interface IMarketDataProvider
{
    Task<IReadOnlyCollection<MarketCandle>> GetLatestCandlesAsync(
        string symbol,
        string timeframe,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken);
}
