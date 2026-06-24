using MarketRadar.Domain.Entities;
using MarketRadar.Domain.Enums;
using MarketRadar.Domain.Results;
using MarketRadar.Domain.Settings;

namespace MarketRadar.Domain.Services;

public class TrendLineService : ITrendLineService
{
    public TrendLineResult Calculate(IReadOnlyList<MarketCandle> candles, TrendAnalysisSettings settings)
    {
        int n = candles.Count;

        // Least squares: y = intercept + slope * x, x = candle index (0..n-1), y = ClosePrice
        decimal sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;

        for (int i = 0; i < n; i++)
        {
            decimal x = i;
            decimal y = candles[i].ClosePrice;
            sumX  += x;
            sumY  += y;
            sumXY += x * y;
            sumX2 += x * x;
        }

        decimal denominator = n * sumX2 - sumX * sumX;
        decimal slope     = denominator == 0 ? 0 : (n * sumXY - sumX * sumY) / denominator;
        decimal intercept = (sumY - slope * sumX) / n;

        // Residual standard deviation: sqrt(sum(residual²) / n)
        decimal sumResidualSquared = 0;
        for (int i = 0; i < n; i++)
        {
            decimal trendPrice = intercept + slope * i;
            decimal residual   = candles[i].ClosePrice - trendPrice;
            sumResidualSquared += residual * residual;
        }
        decimal residualStdDev = (decimal)Math.Sqrt((double)(sumResidualSquared / n));

        decimal currentTrendPrice  = intercept + slope * (n - 1);
        decimal currentClosePrice  = candles[n - 1].ClosePrice;
        decimal distanceFromLine   = currentClosePrice - currentTrendPrice;

        TrendDirection direction;
        if (slope > settings.MinimumSlope)
            direction = TrendDirection.Up;
        else if (slope < -settings.MinimumSlope)
            direction = TrendDirection.Down;
        else
            direction = TrendDirection.Neutral;

        return new TrendLineResult(
            Slope:                      slope,
            Intercept:                  intercept,
            CurrentTrendPrice:          currentTrendPrice,
            CurrentClosePrice:          currentClosePrice,
            DistanceFromTrendLine:      distanceFromLine,
            ResidualStandardDeviation:  residualStdDev,
            Direction:                  direction);
    }
}
