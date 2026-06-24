using MarketRadar.Application.Contracts.Services;
using MarketRadar.Domain.Entities;

namespace MarketRadar.Application.Tests.Helpers;

internal sealed class CapturingAlertDispatcher : IAlertDispatcher
{
    public List<MarketAlert> Dispatched { get; } = [];

    public Task DispatchAsync(MarketAlert alert, CancellationToken cancellationToken)
    {
        Dispatched.Add(alert);
        return Task.CompletedTask;
    }
}
