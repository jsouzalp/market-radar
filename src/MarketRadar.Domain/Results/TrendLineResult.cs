using MarketRadar.Domain.Enums;

namespace MarketRadar.Domain.Results;

public sealed record TrendLineResult(
    decimal Slope,
    decimal Intercept,
    decimal CurrentTrendPrice,
    decimal CurrentClosePrice,
    decimal DistanceFromTrendLine,
    decimal ResidualStandardDeviation,
    TrendDirection Direction);
