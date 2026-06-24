using MarketRadar.Domain.Entities;

namespace MarketRadar.Application.Contracts.Services;

public interface IAlertNotificationService
{
    event Action<MarketAlert>? AlertReceived;
    void Notify(MarketAlert alert);
}
