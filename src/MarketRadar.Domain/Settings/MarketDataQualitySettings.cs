namespace MarketRadar.Domain.Settings;

public class MarketDataQualitySettings
{
    public int MinimumRequiredCandles { get; set; }
    public int StaleDataToleranceMinutes { get; set; }
    public bool UseOnlyClosedCandlesForAnalysis { get; set; }
}
