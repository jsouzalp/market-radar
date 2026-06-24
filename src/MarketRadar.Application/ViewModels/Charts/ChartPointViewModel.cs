using MarketRadar.Domain.Enums;

namespace MarketRadar.Application.ViewModels.Charts;

public class ChartPointViewModel
{
    public DateTime Time { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal? TrendLinePrice { get; set; }
    public decimal? Ema9 { get; set; }
    public decimal? Ema21 { get; set; }
    public decimal? Ema50 { get; set; }
    public bool HasAlert { get; set; }
    public AlertType? AlertType { get; set; }
}
