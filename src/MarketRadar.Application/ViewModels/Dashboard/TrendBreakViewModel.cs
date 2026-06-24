namespace MarketRadar.Application.ViewModels.Dashboard;

public class TrendBreakViewModel
{
    public string Symbol { get; set; } = string.Empty;
    public string Timeframe { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool HasAlert { get; set; }
    public string? AlertType { get; set; }
    public string? Severity { get; set; }
    public decimal Score { get; set; }
    public string Message { get; set; } = string.Empty;
}
