namespace MarketRadar.Domain.Entities;

public class MarketCandle
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Timeframe { get; set; } = string.Empty;
    public DateTime OpenTime { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal? Volume { get; set; }
    public DateTime CreatedAt { get; set; }
}
