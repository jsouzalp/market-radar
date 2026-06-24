using MarketRadar.Domain.Entities;
using MarketRadar.Domain.Results;
using MarketRadar.Domain.Settings;

namespace MarketRadar.Domain.Services;

public interface IMarketDataQualityService
{
    MarketDataQualityResult Evaluate(
        IReadOnlyCollection<MarketCandle> candles,
        MarketDataQualitySettings settings);
}
