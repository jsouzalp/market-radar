using MarketRadar.Domain.Entities;

namespace MarketRadar.Application.Contracts.Services;

public interface IAlertDispatcher
{
    Task DispatchAsync(
        MarketAlert alert,
        CancellationToken cancellationToken);
}
