namespace MarketRadar.Application.ViewModels.Dashboard;

public class TrendLineViewModel
{
    public decimal Slope { get; set; }
    public decimal Intercept { get; set; }
    public decimal CurrentTrendPrice { get; set; }
    public decimal CurrentClosePrice { get; set; }
    public decimal DistanceFromTrendLine { get; set; }
    public decimal ResidualStandardDeviation { get; set; }
    public string Direction { get; set; } = string.Empty;
}
