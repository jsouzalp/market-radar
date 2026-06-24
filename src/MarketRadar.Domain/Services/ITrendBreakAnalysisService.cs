using MarketRadar.Domain.Entities;
using MarketRadar.Domain.Results;
using MarketRadar.Domain.Settings;

namespace MarketRadar.Domain.Services;

public interface ITrendBreakAnalysisService
{
    TrendBreakAnalysisResult Analyze(
        IReadOnlyList<MarketCandle> candles,
        TrendAnalysisSettings trendSettings,
        MovingAverageSettings movingAverageSettings);
}
