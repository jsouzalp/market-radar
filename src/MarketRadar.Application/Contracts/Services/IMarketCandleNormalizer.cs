using MarketRadar.Domain.Entities;

namespace MarketRadar.Application.Contracts.Services;

public interface IMarketCandleNormalizer
{
    IReadOnlyCollection<MarketCandle> Normalize(
        IReadOnlyCollection<MarketCandle> candles,
        string internalSymbol,
        string internalTimeframe);
}
