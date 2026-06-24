namespace MarketRadar.Domain.Settings;

public class MovingAverageSettings
{
    public IReadOnlyCollection<int> Periods { get; set; } = Array.Empty<int>();
}
