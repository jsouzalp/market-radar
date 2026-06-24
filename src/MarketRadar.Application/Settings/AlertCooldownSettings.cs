namespace MarketRadar.Application.Settings;

public class AlertCooldownSettings
{
    public int MaxConsecutiveAlerts { get; set; } = 5;
    public int MinCyclesToReset { get; set; } = 3;
}
