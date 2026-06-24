using MarketRadar.Application.Contracts.Services;
using MarketRadar.Domain.Entities;

namespace MarketRadar.Application.Services;

public sealed class MarketCandleNormalizer : IMarketCandleNormalizer
{
    private readonly ISymbolNormalizer _symbolNormalizer;
    private readonly ITimeframeNormalizer _timeframeNormalizer;

    public MarketCandleNormalizer(
        ISymbolNormalizer symbolNormalizer,
        ITimeframeNormalizer timeframeNormalizer)
    {
        _symbolNormalizer    = symbolNormalizer;
        _timeframeNormalizer = timeframeNormalizer;
    }

    public IReadOnlyCollection<MarketCandle> Normalize(
        IReadOnlyCollection<MarketCandle> candles,
        string internalSymbol,
        string internalTimeframe)
    {
        var normalizedSymbol    = _symbolNormalizer.Normalize(internalSymbol);
        var normalizedTimeframe = _timeframeNormalizer.Normalize(internalTimeframe);

        foreach (var candle in candles)
        {
            candle.Symbol    = normalizedSymbol;
            candle.Timeframe = normalizedTimeframe;

            candle.OpenTime = candle.OpenTime.Kind switch
            {
                DateTimeKind.Local      => candle.OpenTime.ToUniversalTime(),
                DateTimeKind.Unspecified => DateTime.SpecifyKind(candle.OpenTime, DateTimeKind.Utc),
                _                       => candle.OpenTime
            };
        }

        return candles.OrderBy(c => c.OpenTime).ToList().AsReadOnly();
    }
}
