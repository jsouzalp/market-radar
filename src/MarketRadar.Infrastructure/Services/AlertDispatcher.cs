using MarketRadar.Application.Contracts.Services;
using MarketRadar.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MarketRadar.Infrastructure.Services;

public sealed class AlertDispatcher : IAlertDispatcher
{
    private readonly IAlertNotificationService _notificationService;
    private readonly ILogger<AlertDispatcher> _logger;

    public AlertDispatcher(
        IAlertNotificationService notificationService,
        ILogger<AlertDispatcher> logger)
    {
        _notificationService = notificationService;
        _logger              = logger;
    }

    public Task DispatchAsync(MarketAlert alert, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Alert dispatched: {AlertType} {Symbol} Severity={Severity} Score={Score}",
            alert.AlertType, alert.Symbol, alert.Severity, alert.Score);

        _notificationService.Notify(alert);
        return Task.CompletedTask;
    }
}
