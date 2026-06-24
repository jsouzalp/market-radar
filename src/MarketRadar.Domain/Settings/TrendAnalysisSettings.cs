namespace MarketRadar.Domain.Settings;

public class TrendAnalysisSettings
{
    public int RegressionWindowCandles { get; set; }
    public int RequiredBreakCandles { get; set; }
    public decimal DeviationMultiplier { get; set; }
    public decimal MinimumSlope { get; set; }
    public decimal MinimumBreakScore { get; set; }
    public int RecentHighLowWindowCandles { get; set; }
    public int FalseBreakoutLookbackCandles { get; set; }
}
