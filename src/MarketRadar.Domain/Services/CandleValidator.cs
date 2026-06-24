using MarketRadar.Domain.Entities;
using MarketRadar.Domain.Results;

namespace MarketRadar.Domain.Services;

public class CandleValidator : ICandleValidator
{
    public CandleValidationResult Validate(MarketCandle candle)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(candle.Symbol))
            errors.Add("Symbol cannot be empty.");

        if (string.IsNullOrWhiteSpace(candle.Timeframe))
            errors.Add("Timeframe cannot be empty.");

        if (candle.OpenTime.Kind != DateTimeKind.Utc)
            errors.Add("OpenTime must be in UTC.");

        if (candle.OpenTime > DateTime.UtcNow)
            errors.Add("OpenTime cannot be in the future.");

        if (candle.OpenPrice <= 0)
            errors.Add("OpenPrice must be greater than zero.");

        if (candle.HighPrice <= 0)
            errors.Add("HighPrice must be greater than zero.");

        if (candle.LowPrice <= 0)
            errors.Add("LowPrice must be greater than zero.");

        if (candle.ClosePrice <= 0)
            errors.Add("ClosePrice must be greater than zero.");

        if (candle.HighPrice < candle.LowPrice)
            errors.Add("HighPrice must be greater than or equal to LowPrice.");

        // Only validate OHLC consistency when individual prices are valid
        if (errors.Count == 0)
        {
            if (candle.OpenPrice < candle.LowPrice || candle.OpenPrice > candle.HighPrice)
                errors.Add("OpenPrice must be between LowPrice and HighPrice.");

            if (candle.ClosePrice < candle.LowPrice || candle.ClosePrice > candle.HighPrice)
                errors.Add("ClosePrice must be between LowPrice and HighPrice.");
        }

        if (candle.Volume.HasValue && candle.Volume.Value < 0)
            errors.Add("Volume cannot be negative.");

        return new CandleValidationResult(errors.Count == 0, errors.AsReadOnly());
    }
}
