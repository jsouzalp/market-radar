using MarketRadar.Application.ViewModels.Alerts;
using MarketRadar.Application.ViewModels.Charts;

namespace MarketRadar.Application.ViewModels.Dashboard;

public class DashboardViewModel
{
    public string Symbol { get; set; } = string.Empty;
    public string Timeframe { get; set; } = string.Empty;
    public DashboardStatus Status { get; set; }
    public decimal? CurrentPrice { get; set; }
    public decimal? PreviousClosePrice { get; set; }
    public decimal? AbsoluteVariation { get; set; }
    public decimal? PercentageVariation { get; set; }
    public TrendLineViewModel? TrendLine { get; set; }
    public IReadOnlyCollection<MovingAverageViewModel> MovingAverages { get; set; } = Array.Empty<MovingAverageViewModel>();
    public TrendBreakViewModel? CurrentAnalysis { get; set; }
    public MarketAlertViewModel? LastAlert { get; set; }
    public IReadOnlyCollection<MarketAlertViewModel> RecentAlerts { get; set; } = Array.Empty<MarketAlertViewModel>();
    public IReadOnlyCollection<ChartPointViewModel> ChartPoints { get; set; } = Array.Empty<ChartPointViewModel>();
    public DateTime? LastUpdatedAt { get; set; }

    // WaitingForEnoughData progress
    public int CandleCount { get; set; }
    public int MinimumRequiredCandles { get; set; }

    // Alert cooldown
    public int CooldownConsecutiveCount { get; set; }
    public int CooldownMaxCount { get; set; }
    public bool IsCooldownActive { get; set; }
}
