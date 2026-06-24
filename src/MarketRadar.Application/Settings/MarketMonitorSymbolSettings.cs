namespace MarketRadar.Application.Settings;

public class MarketMonitorSymbolSettings
{
    public string Code { get; set; } = string.Empty;
    public string Timeframe { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}
