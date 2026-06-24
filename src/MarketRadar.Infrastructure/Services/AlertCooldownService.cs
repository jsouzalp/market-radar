using System.Collections.Concurrent;
using MarketRadar.Application.Contracts.Services;
using MarketRadar.Application.Results;
using MarketRadar.Application.Settings;

namespace MarketRadar.Infrastructure.Services;

public sealed class AlertCooldownService : IAlertCooldownService
{
    private record State(int Consecutive, int Quiet);

    private readonly ConcurrentDictionary<(string Symbol, string AlertType), State> _states = new();

    public AlertCooldownResult Evaluate(
        string symbol,
        string alertType,
        bool hasBreakout,
        AlertCooldownSettings settings)
    {
        var key = (symbol, alertType);

        if (hasBreakout)
        {
            var next = _states.AddOrUpdate(
                key,
                _ => new State(1, 0),
                (_, s) => new State(s.Consecutive + 1, 0));

            return Build(next, settings, canDispatch: next.Consecutive <= settings.MaxConsecutiveAlerts);
        }
        else
        {
            // No breakout: increment quiet counter; reset if threshold reached
            var next = _states.AddOrUpdate(
                key,
                _ => new State(0, 0),
                (_, s) =>
                {
                    var quiet = s.Quiet + 1;
                    return quiet >= settings.MinCyclesToReset
                        ? new State(0, 0)
                        : new State(s.Consecutive, quiet);
                });

            return Build(next, settings, canDispatch: true);
        }
    }

    public AlertCooldownResult GetCurrentState(
        string symbol,
        string alertType,
        AlertCooldownSettings settings)
    {
        var state = _states.TryGetValue((symbol, alertType), out var s) ? s : new State(0, 0);
        return Build(state, settings, canDispatch: state.Consecutive <= settings.MaxConsecutiveAlerts);
    }

    private static AlertCooldownResult Build(State s, AlertCooldownSettings settings, bool canDispatch) =>
        new()
        {
            CanDispatch          = canDispatch,
            ConsecutiveCount     = s.Consecutive,
            MaxConsecutiveAlerts = settings.MaxConsecutiveAlerts,
            QuietCyclesCount     = s.Quiet,
            MinCyclesToReset     = settings.MinCyclesToReset
        };
}
