using MarketRadar.Application.Contracts.Services;
using MarketRadar.Domain.Entities;

namespace MarketRadar.Infrastructure.Services;

public sealed class AlertNotificationService : IAlertNotificationService
{
    public event Action<MarketAlert>? AlertReceived;

    public void Notify(MarketAlert alert) => AlertReceived?.Invoke(alert);
}
