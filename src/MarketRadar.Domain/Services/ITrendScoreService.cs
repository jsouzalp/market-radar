using MarketRadar.Domain.Entities;
using MarketRadar.Domain.Results;
using MarketRadar.Domain.Settings;

namespace MarketRadar.Domain.Services;

public interface ITrendScoreService
{
    decimal CalculateScore(
        TrendLineResult trendLine,
        IReadOnlyCollection<MovingAverageResult> movingAverages,
        IReadOnlyList<MarketCandle> candles,
        TrendAnalysisSettings settings);
}
