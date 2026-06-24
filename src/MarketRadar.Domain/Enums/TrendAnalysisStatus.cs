namespace MarketRadar.Domain.Enums;

public enum TrendAnalysisStatus
{
    Unknown           = 0,
    Success           = 1,
    InsufficientData  = 2,
    NeutralTrend      = 3,
    NoBreakout        = 4,
    BreakoutDetected  = 5,
    QualityBlocked    = 6
}
