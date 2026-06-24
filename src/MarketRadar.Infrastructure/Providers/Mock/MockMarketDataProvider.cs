using MarketRadar.Application.Contracts.Providers;
using MarketRadar.Application.Settings;
using MarketRadar.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MarketRadar.Infrastructure.Providers.Mock;

public sealed class MockMarketDataProvider : IMarketDataProvider
{
    private readonly MarketDataProviderSettings _settings;
    private readonly ILogger<MockMarketDataProvider> _logger;

    public MockMarketDataProvider(
        MarketDataProviderSettings settings,
        ILogger<MockMarketDataProvider> logger)
    {
        _settings = settings;
        _logger   = logger;
    }

    public Task<IReadOnlyCollection<MarketCandle>> GetLatestCandlesAsync(
        string symbol,
        string timeframe,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        if (_settings.MockScenario == "ProviderFailure")
            throw new InvalidOperationException("Simulated provider failure.");

        if (_settings.MockScenario is "EmptyResponse" or "MarketClosed")
            return Task.FromResult<IReadOnlyCollection<MarketCandle>>(Array.Empty<MarketCandle>());

        // Cap at 500 candles
        var effectiveFrom = from < to.AddMinutes(-500) ? to.AddMinutes(-500) : from;
        var candles       = new List<MarketCandle>();
        var openTime      = effectiveFrom;

        while (openTime < to)
        {
            var price = ComputePrice(openTime, to);
            candles.Add(new MarketCandle
            {
                Symbol     = symbol,
                Timeframe  = timeframe,
                OpenTime   = openTime,
                OpenPrice  = price,
                HighPrice  = price + 0.5m,
                LowPrice   = price - 0.5m,
                ClosePrice = price,
                Volume     = 1000m,
                CreatedAt  = DateTime.UtcNow
            });
            openTime = openTime.AddMinutes(1);
        }

        _logger.LogDebug("MockProvider generated {Count} candles for {Symbol} scenario={Scenario}",
            candles.Count, symbol, _settings.MockScenario);

        return Task.FromResult<IReadOnlyCollection<MarketCandle>>(candles.AsReadOnly());
    }

    private decimal ComputePrice(DateTime openTime, DateTime to)
    {
        double minutesFromEnd = (to - openTime).TotalMinutes;

        return _settings.MockScenario switch
        {
            "StableMarket"   => 2000m + (decimal)(Math.Sin(minutesFromEnd * 0.2) * 5),
            "UpTrend"        => 1850m + (decimal)(120 - Math.Min(minutesFromEnd, 120)) * 0.3m,
            "DownTrend"      => 2150m - (decimal)(120 - Math.Min(minutesFromEnd, 120)) * 0.3m,
            // Only 5 break candles so the 120-candle regression slope stays positive (uptrend)
            "TrendBreakDown" => minutesFromEnd > 5
                                    ? 1800m + Math.Max(0m, (decimal)(120 - Math.Min(minutesFromEnd, 120))) * 0.5m
                                    : 1750m,
            "TrendBreakUp"   => minutesFromEnd > 5
                                    ? 2200m - Math.Max(0m, (decimal)(120 - Math.Min(minutesFromEnd, 120))) * 0.5m
                                    : 2260m,
            _                => 2000m
        };
    }
}
