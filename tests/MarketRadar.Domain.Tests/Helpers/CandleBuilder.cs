using MarketRadar.Domain.Entities;

namespace MarketRadar.Domain.Tests.Helpers;

internal static class CandleBuilder
{
    public static MarketCandle ValidCandle(
        string symbol = "XAUUSD", string timeframe = "M1",
        decimal price = 100m, decimal? volume = 1000m,
        DateTime? openTime = null) => new()
    {
        Symbol    = symbol,
        Timeframe = timeframe,
        OpenTime  = openTime ?? DateTime.UtcNow.AddMinutes(-5),
        OpenPrice  = price,
        HighPrice  = price + 1m,
        LowPrice   = Math.Max(0.01m, price - 1m),
        ClosePrice = price,
        Volume     = volume,
        CreatedAt  = DateTime.UtcNow
    };

    public static IReadOnlyList<MarketCandle> LinearSequence(
        int count, decimal startPrice, decimal priceStep,
        string symbol = "XAUUSD", string timeframe = "M1",
        DateTime? baseTime = null)
    {
        var t0   = baseTime ?? DateTime.UtcNow.AddMinutes(-count - 10);
        var list = new List<MarketCandle>(count);
        for (int i = 0; i < count; i++)
        {
            decimal close = startPrice + i * priceStep;
            list.Add(new MarketCandle
            {
                Symbol    = symbol,
                Timeframe = timeframe,
                OpenTime  = t0.AddMinutes(i),
                OpenPrice  = close,
                HighPrice  = close + 1m,
                LowPrice   = Math.Max(0.01m, close - 1m),
                ClosePrice = close,
                Volume     = 1000m,
                CreatedAt  = DateTime.UtcNow
            });
        }
        return list.AsReadOnly();
    }

    public static IReadOnlyList<MarketCandle> Uptrend(int count = 5, decimal start = 100m, decimal step = 1m)
        => LinearSequence(count, start, step);

    public static IReadOnlyList<MarketCandle> Downtrend(int count = 5, decimal start = 104m, decimal step = 1m)
        => LinearSequence(count, start, -step);

    public static IReadOnlyList<MarketCandle> Flat(int count = 5, decimal price = 100m)
        => LinearSequence(count, price, 0m);

    public static IReadOnlyList<MarketCandle> BreakoutDown(
        int trendCount = 50, int breakCount = 3,
        decimal startPrice = 100m, decimal trendStep = 2m, decimal breakPrice = 140m,
        string symbol = "XAUUSD", string timeframe = "M1")
    {
        var list  = new List<MarketCandle>(trendCount + breakCount);
        var trend = LinearSequence(trendCount, startPrice, trendStep, symbol, timeframe);
        list.AddRange(trend);
        var lastTime = trend[^1].OpenTime;
        for (int i = 0; i < breakCount; i++)
            list.Add(new MarketCandle
            {
                Symbol    = symbol,
                Timeframe = timeframe,
                OpenTime  = lastTime.AddMinutes(i + 1),
                OpenPrice  = breakPrice,
                HighPrice  = breakPrice + 1m,
                LowPrice   = breakPrice - 1m,
                ClosePrice = breakPrice,
                Volume     = 1000m,
                CreatedAt  = DateTime.UtcNow
            });
        return list.AsReadOnly();
    }

    public static IReadOnlyList<MarketCandle> BreakoutUp(
        int trendCount = 50, int breakCount = 3,
        decimal startPrice = 200m, decimal trendStep = 2m, decimal breakPrice = 160m,
        string symbol = "XAUUSD", string timeframe = "M1")
    {
        var list  = new List<MarketCandle>(trendCount + breakCount);
        var trend = LinearSequence(trendCount, startPrice, -trendStep, symbol, timeframe);
        list.AddRange(trend);
        var lastTime = trend[^1].OpenTime;
        for (int i = 0; i < breakCount; i++)
            list.Add(new MarketCandle
            {
                Symbol    = symbol,
                Timeframe = timeframe,
                OpenTime  = lastTime.AddMinutes(i + 1),
                OpenPrice  = breakPrice,
                HighPrice  = breakPrice + 1m,
                LowPrice   = breakPrice - 1m,
                ClosePrice = breakPrice,
                Volume     = 1000m,
                CreatedAt  = DateTime.UtcNow
            });
        return list.AsReadOnly();
    }
}
