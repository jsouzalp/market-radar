namespace MarketRadar.Application.ViewModels.Alerts;

public class MarketAlertViewModel
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Timeframe { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string TimeDisplay { get; set; } = string.Empty;
}
