using MarketRadar.Domain.Entities;
using MarketRadar.Domain.Enums;
using MarketRadar.Domain.Results;
using MarketRadar.Domain.Settings;

namespace MarketRadar.Domain.Services;

public class MarketDataQualityService : IMarketDataQualityService
{
    public MarketDataQualityResult Evaluate(
        IReadOnlyCollection<MarketCandle> candles,
        MarketDataQualitySettings settings)
    {
        if (candles.Count == 0)
            return new MarketDataQualityResult(
                MarketDataQualityStatus.WaitingForEnoughData,
                CanAnalyze: false,
                Messages: ["No candles available for analysis."]);

        if (candles.Count < settings.MinimumRequiredCandles)
            return new MarketDataQualityResult(
                MarketDataQualityStatus.WaitingForEnoughData,
                CanAnalyze: false,
                Messages: [$"Insufficient candles: {candles.Count} of {settings.MinimumRequiredCandles} required."]);

        var lastCandle = candles.MaxBy(c => c.OpenTime)!;
        var staleness = DateTime.UtcNow - lastCandle.OpenTime;

        if (staleness.TotalMinutes > settings.StaleDataToleranceMinutes)
            return new MarketDataQualityResult(
                MarketDataQualityStatus.StaleData,
                CanAnalyze: false,
                Messages: [$"Last candle is {staleness.TotalMinutes:F0} minutes old. Tolerance is {settings.StaleDataToleranceMinutes} minutes."]);

        return new MarketDataQualityResult(
            MarketDataQualityStatus.Valid,
            CanAnalyze: true,
            Messages: Array.Empty<string>());
    }
}
