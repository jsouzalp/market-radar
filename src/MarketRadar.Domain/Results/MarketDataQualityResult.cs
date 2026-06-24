using MarketRadar.Domain.Enums;

namespace MarketRadar.Domain.Results;

public sealed record MarketDataQualityResult(
    MarketDataQualityStatus Status,
    bool CanAnalyze,
    IReadOnlyCollection<string> Messages);
