using MarketRadar.Domain.Enums;

namespace MarketRadar.Domain.Results;

public sealed record MovingAverageResult(
    int Period,
    decimal CurrentValue,
    decimal PreviousValue,
    MovingAverageDirection Direction,
    bool CurrentPriceIsAbove);
