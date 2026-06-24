using MarketRadar.Application.Results;
using MarketRadar.Application.Settings;

namespace MarketRadar.Application.Contracts.Services;

public interface IAlertCooldownService
{
    /// <summary>
    /// Called every analysis cycle. Updates internal state and returns the dispatch decision.
    /// </summary>
    AlertCooldownResult Evaluate(
        string symbol,
        string alertType,
        bool hasBreakout,
        AlertCooldownSettings settings);

    /// <summary>
    /// Returns the current cooldown state without modifying it (used by the dashboard).
    /// </summary>
    AlertCooldownResult GetCurrentState(string symbol, string alertType, AlertCooldownSettings settings);
}
