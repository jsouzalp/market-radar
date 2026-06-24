namespace MarketRadar.Application.Settings;

public class MarketDataProviderSettings
{
    public string Primary { get; set; } = "Mock";
    public string? Comparison { get; set; }
    public string ActiveProvider { get; set; } = "Mock";
    public int PollingIntervalSeconds { get; set; } = 60;
    public string MockScenario { get; set; } = "TrendBreakDown";
    public bool UseOnlyClosedCandles { get; set; } = true;
    public int RequestOverlapMinutes { get; set; } = 5;
}
