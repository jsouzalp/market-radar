using MarketRadar.Domain.Entities;
using MarketRadar.Domain.Results;
using MarketRadar.Domain.Settings;

namespace MarketRadar.Domain.Services;

public interface ITrendLineService
{
    TrendLineResult Calculate(IReadOnlyList<MarketCandle> candles, TrendAnalysisSettings settings);
}
