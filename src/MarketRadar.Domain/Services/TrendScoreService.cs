using MarketRadar.Domain.Entities;
using MarketRadar.Domain.Enums;
using MarketRadar.Domain.Results;
using MarketRadar.Domain.Settings;

namespace MarketRadar.Domain.Services;

public class TrendScoreService : ITrendScoreService
{
    public decimal CalculateScore(
        TrendLineResult trendLine,
        IReadOnlyCollection<MovingAverageResult> movingAverages,
        IReadOnlyList<MarketCandle> candles,
        TrendAnalysisSettings settings)
    {
        if (trendLine.Direction == TrendDirection.Neutral)
            return 0m;

        bool isBreakDown    = trendLine.Direction == TrendDirection.Up;
        decimal score       = 0m;
        int n               = candles.Count;
        decimal currentClose = candles[n - 1].ClosePrice;

        // +25: rompeu a linha de regressão (current candle)
        bool brokeLine = isBreakDown
            ? currentClose < trendLine.CurrentTrendPrice
            : currentClose > trendLine.CurrentTrendPrice;
        if (brokeLine) score += 25m;

        // +20: manteve rompimento por K candles seguidos
        int k = Math.Min(settings.RequiredBreakCandles, n);
        bool maintainedBreak = true;
        for (int i = n - k; i < n; i++)
        {
            decimal trendPriceAtI = trendLine.Intercept + trendLine.Slope * i;
            bool brokeAtI = isBreakDown
                ? candles[i].ClosePrice < trendPriceAtI
                : candles[i].ClosePrice > trendPriceAtI;
            if (!brokeAtI) { maintainedBreak = false; break; }
        }
        if (maintainedBreak) score += 20m;

        // +20: distância média dos últimos K candles > breakThreshold
        decimal breakThreshold = trendLine.ResidualStandardDeviation * settings.DeviationMultiplier;
        decimal totalDistance = 0m;
        for (int i = n - k; i < n; i++)
        {
            decimal trendPriceAtI = trendLine.Intercept + trendLine.Slope * i;
            decimal dist = isBreakDown
                ? trendPriceAtI - candles[i].ClosePrice   // positive when below line
                : candles[i].ClosePrice - trendPriceAtI;  // positive when above line
            totalDistance += dist;
        }
        if (k > 0 && totalDistance / k > breakThreshold) score += 20m;

        // +15: preço rompeu a EMA 21
        var ema21 = movingAverages.FirstOrDefault(m => m.Period == 21);
        if (ema21 != null)
        {
            bool brokeEma21 = isBreakDown
                ? currentClose < ema21.CurrentValue
                : currentClose > ema21.CurrentValue;
            if (brokeEma21) score += 15m;
        }

        // +10: EMA 9 virou contra a tendência anterior
        var ema9 = movingAverages.FirstOrDefault(m => m.Period == 9);
        if (ema9 != null)
        {
            bool ema9TurnedAgainst = isBreakDown
                ? ema9.Direction == MovingAverageDirection.Down
                : ema9.Direction == MovingAverageDirection.Up;
            if (ema9TurnedAgainst) score += 10m;
        }

        // +10: rompeu máxima ou mínima recente (RecentHighLowWindowCandles, excluindo candle atual)
        int windowEnd   = n - 2;
        int windowStart = Math.Max(0, windowEnd - settings.RecentHighLowWindowCandles + 1);
        if (windowEnd >= 0 && windowEnd >= windowStart)
        {
            if (isBreakDown)
            {
                decimal recentLow = decimal.MaxValue;
                for (int i = windowStart; i <= windowEnd; i++)
                    recentLow = Math.Min(recentLow, candles[i].LowPrice);
                if (currentClose < recentLow) score += 10m;
            }
            else
            {
                decimal recentHigh = decimal.MinValue;
                for (int i = windowStart; i <= windowEnd; i++)
                    recentHigh = Math.Max(recentHigh, candles[i].HighPrice);
                if (currentClose > recentHigh) score += 10m;
            }
        }

        return Math.Min(score, 100m);
    }
}
