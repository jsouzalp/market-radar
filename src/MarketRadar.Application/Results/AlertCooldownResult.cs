namespace MarketRadar.Application.Results;

public record AlertCooldownResult
{
    public bool CanDispatch { get; init; } = true;
    public int ConsecutiveCount { get; init; }
    public int MaxConsecutiveAlerts { get; init; }
    public int QuietCyclesCount { get; init; }
    public int MinCyclesToReset { get; init; }
    // Blocked means the NEXT dispatch is denied; when Consecutive == Max, dispatch was still allowed
    public bool IsBlocked => MaxConsecutiveAlerts > 0 && ConsecutiveCount > MaxConsecutiveAlerts;
}
