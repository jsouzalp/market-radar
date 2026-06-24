namespace MarketRadar.Application.ViewModels.Dashboard;

public class MovingAverageViewModel
{
    public int Period { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal PreviousValue { get; set; }
    public string Direction { get; set; } = string.Empty;
    public bool CurrentPriceIsAbove { get; set; }
}
