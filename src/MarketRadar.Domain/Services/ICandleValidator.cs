using MarketRadar.Domain.Entities;
using MarketRadar.Domain.Results;

namespace MarketRadar.Domain.Services;

public interface ICandleValidator
{
    CandleValidationResult Validate(MarketCandle candle);
}
