using MarketRadar.Domain.Enums;

namespace MarketRadar.Domain.Results;

public sealed record TrendBreakAnalysisResult(
    string Symbol,
    string Timeframe,
    TrendAnalysisStatus Status,
    bool HasAlert,
    AlertType? AlertType,
    AlertSeverity? Severity,
    decimal Score,
    TrendLineResult TrendLine,
    IReadOnlyCollection<MovingAverageResult> MovingAverages,
    string Message);
