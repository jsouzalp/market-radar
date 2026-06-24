using MarketRadar.Domain.Entities;
using MarketRadar.Domain.Enums;
using MarketRadar.Domain.Results;

namespace MarketRadar.Domain.Services;

public class MovingAverageService : IMovingAverageService
{
    public IReadOnlyCollection<MovingAverageResult> CalculateEma(
        IReadOnlyList<MarketCandle> candles,
        IReadOnlyCollection<int> periods)
    {
        var results = new List<MovingAverageResult>(periods.Count);

        foreach (int period in periods)
        {
            if (candles.Count < period)
                continue;

            // Seed with SMA of first `period` candles
            decimal sma = 0;
            for (int i = 0; i < period; i++)
                sma += candles[i].ClosePrice;
            sma /= period;

            decimal multiplier  = 2m / (period + 1);
            decimal previousEma = sma;
            decimal currentEma  = sma;

            // EMA[i] = (Close[i] - EMA[i-1]) * multiplier + EMA[i-1]
            for (int i = period; i < candles.Count; i++)
            {
                previousEma = currentEma;
                currentEma  = (candles[i].ClosePrice - currentEma) * multiplier + currentEma;
            }

            decimal currentClose = candles[candles.Count - 1].ClosePrice;

            MovingAverageDirection direction;
            if (currentEma > previousEma)
                direction = MovingAverageDirection.Up;
            else if (currentEma < previousEma)
                direction = MovingAverageDirection.Down;
            else
                direction = MovingAverageDirection.Neutral;

            results.Add(new MovingAverageResult(
                Period:             period,
                CurrentValue:       currentEma,
                PreviousValue:      previousEma,
                Direction:          direction,
                CurrentPriceIsAbove: currentClose > currentEma));
        }

        return results.AsReadOnly();
    }
}
