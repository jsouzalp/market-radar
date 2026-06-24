using MarketRadar.Domain.Enums;

namespace MarketRadar.Domain.Entities;

public class MarketAlert
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Timeframe { get; set; } = string.Empty;
    public AlertType AlertType { get; set; }
    public AlertSeverity Severity { get; set; }
    public decimal Score { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
