using MarketRadar.Domain.Entities;

namespace MarketRadar.Application.Contracts.Repositories;

public interface IAlertRepository
{
    Task AddAsync(
        MarketAlert alert,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<MarketAlert>> GetRecentAsync(
        string symbol,
        int count,
        CancellationToken cancellationToken);
}
