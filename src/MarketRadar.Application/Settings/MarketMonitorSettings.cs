namespace MarketRadar.Application.Settings;

public class MarketMonitorSettings
{
    public int HistoryDays { get; set; } = 30;
    public IReadOnlyList<MarketMonitorSymbolSettings> Symbols { get; set; } = [];
}
